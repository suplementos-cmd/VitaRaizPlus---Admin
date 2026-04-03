namespace VitaRaiz.Application.Interfaces;

public interface IPermissionsRepository
{
    /// <summary>
    /// Returns all permission codes assigned to the user's role.
    /// </summary>
    Task<List<string>> GetUserPermissionsAsync(int userId);
}
