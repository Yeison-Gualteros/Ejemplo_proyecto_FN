
namespace Entities.Exceptions
{
    public sealed class DocumentoNotFoundException : NotFoundException
    {
        public DocumentoNotFoundException(int Id_Documento)
            : base($"El documento con Id_Documento: {Id_Documento} no fue encontrado.")
        {
        }
    }
}
