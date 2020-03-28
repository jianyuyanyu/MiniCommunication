using System.Threading.Tasks;
using MiniSocket.Transmitting;

namespace MiniSocket.Server
{
    public delegate Task<RequestResult> RequestHandler<TEventArgs>(object sender, TEventArgs e);
}