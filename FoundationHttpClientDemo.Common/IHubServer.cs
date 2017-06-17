using System.Threading.Tasks;

namespace FoundationHttpClientDemo.Common
{
    public interface IHubServer
    {
        Task SayHelloAsync(string message);
    }
}