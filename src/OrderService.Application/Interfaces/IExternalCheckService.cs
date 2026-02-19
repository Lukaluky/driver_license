namespace OrderService.Application.Interfaces;

public interface IExternalCheckService
{
    Task<bool> CheckMvdAsync(string iin);
    Task<bool> CheckMedicalAsync(string iin);
}
