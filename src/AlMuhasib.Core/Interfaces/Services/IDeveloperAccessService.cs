namespace AlMuhasib.Core.Interfaces.Services;

public interface IDeveloperAccessService
{
    bool VerifyPassword(string password);
    void SetPassword(string newPassword);
}
