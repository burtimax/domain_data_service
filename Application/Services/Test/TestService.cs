namespace Application.Services.Test;

public class TestService : ITestService
{
    public async Task<string> TestTaskAsync()
    {
        return "Test Method";
    }
}