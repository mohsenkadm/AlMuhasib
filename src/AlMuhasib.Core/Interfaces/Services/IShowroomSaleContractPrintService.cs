using AlMuhasib.Core.Models.Print;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IShowroomSaleContractPrintService
{
    void PrintContract(ShowroomSaleContractPrintModel model, int copies = 1);
}
