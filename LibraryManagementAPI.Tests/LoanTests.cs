using System.Net;
using System.Net.Http.Json;
using LibraryManagementAPI.DTOs;
using Xunit;

namespace LibraryManagementAPI.Tests;

public class LoanTests : IClassFixture<LibraryApiFactory>
{
    private readonly LibraryApiFactory _factory;

    public LoanTests(LibraryApiFactory factory) => _factory = factory;

    private static async Task<HttpResponseMessage> BorrowAsync(HttpClient client, Guid bookId) =>
        await client.PostAsync($"/api/v1/Books/{bookId}/borrow", null);

    private static async Task<LoanReadDto> BorrowOkAsync(HttpClient client, Guid bookId)
    {
        var response = await BorrowAsync(client, bookId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<LoanReadDto>>())!.Data!;
    }

    private static async Task<List<LoanReadDto>> MyLoansAsync(HttpClient client, bool includeReturned = false) =>
        (await client.GetFromJsonAsync<ApiResponse<List<LoanReadDto>>>($"/api/v1/Loans/me?includeReturned={includeReturned}"))!.Data!;

    [Fact]
    public async Task BorrowAndReturn_UpdatesAvailableCopies_AndLoanStatus()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var member = await TestApi.NewMemberAsync(_factory);
        var book = await TestApi.CreateBookAsync(admin, totalCopies: 2);
        Assert.Equal(2, book.AvailableCopies);

        var loan = await BorrowOkAsync(member, book.Id);
        Assert.Equal("Active", loan.Status);
        Assert.Equal(book.Title, loan.BookTitle);
        Assert.Equal(14, (loan.DueAt - loan.BorrowedAt).TotalDays, precision: 3);
        Assert.Equal(1, (await TestApi.GetBookAsync(member, book.Id)).AvailableCopies);
        Assert.Single(await MyLoansAsync(member));

        var returned = await member.PostAsync($"/api/v1/Loans/{loan.Id}/return", null);
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);
        var body = (await returned.Content.ReadFromJsonAsync<ApiResponse<LoanReadDto>>())!.Data!;
        Assert.Equal("Returned", body.Status);
        Assert.NotNull(body.ReturnedAt);

        Assert.Equal(2, (await TestApi.GetBookAsync(member, book.Id)).AvailableCopies);
        Assert.Empty(await MyLoansAsync(member));
        Assert.Single(await MyLoansAsync(member, includeReturned: true));
    }

    [Fact]
    public async Task ReturningTwice_Returns409()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var member = await TestApi.NewMemberAsync(_factory);
        var book = await TestApi.CreateBookAsync(admin);
        var loan = await BorrowOkAsync(member, book.Id);

        await member.PostAsync($"/api/v1/Loans/{loan.Id}/return", null);
        var again = await member.PostAsync($"/api/v1/Loans/{loan.Id}/return", null);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(1, (await TestApi.GetBookAsync(member, book.Id)).AvailableCopies);
    }

    [Fact]
    public async Task BorrowingSameBookTwice_Returns409()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var member = await TestApi.NewMemberAsync(_factory);
        var book = await TestApi.CreateBookAsync(admin, totalCopies: 5);
        await BorrowOkAsync(member, book.Id);

        var second = await BorrowAsync(member, book.Id);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(4, (await TestApi.GetBookAsync(member, book.Id)).AvailableCopies);
    }

    [Fact]
    public async Task WhenNoCopiesLeft_BorrowReturns409()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var first = await TestApi.NewMemberAsync(_factory);
        var second = await TestApi.NewMemberAsync(_factory);
        var book = await TestApi.CreateBookAsync(admin, totalCopies: 1);
        await BorrowOkAsync(first, book.Id);

        var response = await BorrowAsync(second, book.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoanReadDto>>();
        Assert.Contains("No copies", body!.Message);
    }

    [Fact]
    public async Task BookWithZeroCopies_CannotBeBorrowed()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var member = await TestApi.NewMemberAsync(_factory);
        var book = await TestApi.CreateBookAsync(admin, totalCopies: 0);

        Assert.Equal(0, book.AvailableCopies);
        Assert.Equal(HttpStatusCode.Conflict, (await BorrowAsync(member, book.Id)).StatusCode);
    }

    [Fact]
    public async Task MaxActiveLoans_IsEnforced()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var member = await TestApi.NewMemberAsync(_factory);

        for (var i = 0; i < LibraryApiFactory.MaxActiveLoansPerUser; i++)
            await BorrowOkAsync(member, (await TestApi.CreateBookAsync(admin)).Id);

        var oneTooMany = await BorrowAsync(member, (await TestApi.CreateBookAsync(admin)).Id);

        Assert.Equal(HttpStatusCode.Conflict, oneTooMany.StatusCode);
        var body = await oneTooMany.Content.ReadFromJsonAsync<ApiResponse<LoanReadDto>>();
        Assert.Contains($"more than {LibraryApiFactory.MaxActiveLoansPerUser}", body!.Message);
    }

    [Fact]
    public async Task OverdueLoans_AreReported_AndBlockNewBorrowing_UntilReturned()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var member = await TestApi.NewMemberAsync(_factory);
        var book = await TestApi.CreateBookAsync(admin);
        var other = await TestApi.CreateBookAsync(admin);
        var loan = await BorrowOkAsync(member, book.Id);

        _factory.Clock.Advance(TimeSpan.FromDays(16));

        var mine = Assert.Single(await MyLoansAsync(member));
        Assert.Equal("Overdue", mine.Status);
        Assert.Equal(2, mine.DaysOverdue);

        var overdue = (await admin.GetFromJsonAsync<ApiResponse<List<LoanReadDto>>>("/api/v1/Loans/overdue"))!.Data!;
        Assert.Contains(overdue, l => l.Id == loan.Id);

        var blocked = await BorrowAsync(member, other.Id);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Contains("overdue", (await blocked.Content.ReadFromJsonAsync<ApiResponse<LoanReadDto>>())!.Message);

        await member.PostAsync($"/api/v1/Loans/{loan.Id}/return", null);
        await BorrowOkAsync(member, other.Id);
    }

    [Fact]
    public async Task OtherMembers_CannotSeeOrReturnMyLoan_ButAdminCan()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var owner = await TestApi.NewMemberAsync(_factory);
        var stranger = await TestApi.NewMemberAsync(_factory);
        var book = await TestApi.CreateBookAsync(admin);
        var loan = await BorrowOkAsync(owner, book.Id);

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/v1/Loans/{loan.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsync($"/api/v1/Loans/{loan.Id}/return", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/v1/Loans/{loan.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/Loans/{loan.Id}/return", null)).StatusCode);
    }

    [Fact]
    public async Task LoanEndpoints_RequireLogin_AndAdminListsRequireAdmin()
    {
        var anonymous = _factory.CreateClient();
        var member = await TestApi.NewMemberAsync(_factory);

        Assert.Equal(HttpStatusCode.Unauthorized, (await BorrowAsync(anonymous, Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/Loans/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/v1/Loans")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/v1/Loans/overdue")).StatusCode);
    }

    [Fact]
    public async Task BorrowingUnknownBook_Returns404()
    {
        var member = await TestApi.NewMemberAsync(_factory);
        Assert.Equal(HttpStatusCode.NotFound, (await BorrowAsync(member, Guid.NewGuid())).StatusCode);
    }

    [Fact]
    public async Task BookOnLoan_CannotBeDeleted_UntilReturned_AndHistoryKeepsIt()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var member = await TestApi.NewMemberAsync(_factory);
        var book = await TestApi.CreateBookAsync(admin, title: "Soon gone");
        var loan = await BorrowOkAsync(member, book.Id);

        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/v1/Books/{book.Id}")).StatusCode);

        await member.PostAsync($"/api/v1/Loans/{loan.Id}/return", null);
        Assert.Equal(HttpStatusCode.OK, (await admin.DeleteAsync($"/api/v1/Books/{book.Id}")).StatusCode);

        var history = await MyLoansAsync(member, includeReturned: true);
        Assert.Contains(history, l => l.Id == loan.Id && l.BookTitle == "Soon gone");
    }

    [Fact]
    public async Task ChangingTotalCopies_KeepsCopiesOnLoanInAccount()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var a = await TestApi.NewMemberAsync(_factory);
        var b = await TestApi.NewMemberAsync(_factory);
        var book = await TestApi.CreateBookAsync(admin, totalCopies: 3);
        await BorrowOkAsync(a, book.Id);
        await BorrowOkAsync(b, book.Id);

        BookCreateUpdateDto Update(int copies) => new()
        {
            Title = book.Title, ISBN = book.ISBN, PublicationYear = book.PublicationYear,
            AuthorId = book.AuthorId, GenreId = book.GenreId, TotalCopies = copies
        };

        var tooLow = await admin.PutAsJsonAsync($"/api/v1/Books/{book.Id}", Update(1));
        Assert.Equal(HttpStatusCode.BadRequest, tooLow.StatusCode);

        var raised = await admin.PutAsJsonAsync($"/api/v1/Books/{book.Id}", Update(5));
        Assert.Equal(HttpStatusCode.OK, raised.StatusCode);
        var updated = (await raised.Content.ReadFromJsonAsync<ApiResponse<BookReadDto>>())!.Data!;
        Assert.Equal(5, updated.TotalCopies);
        Assert.Equal(3, updated.AvailableCopies);
    }

    [Fact]
    public async Task ParallelBorrowsOfLastCopies_NeverOverLend()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var book = await TestApi.CreateBookAsync(admin, totalCopies: 2);
        var members = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => TestApi.NewMemberAsync(_factory)));

        var responses = await Task.WhenAll(members.Select(m => BorrowAsync(m, book.Id)));

        Assert.Equal(2, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        Assert.Equal(0, (await TestApi.GetBookAsync(admin, book.Id)).AvailableCopies);
    }
}
