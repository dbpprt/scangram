using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Scangram.Common
{
    public abstract class HostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private CancellationTokenSource _cts;
        private Task _executingTask;

        public HostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            using (var scope = _serviceProvider.CreateScope())
            {
                _executingTask = ExecuteAsync(scope.ServiceProvider, _cts.Token);
            }

            return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executingTask == null)
                return;

            _cts.Cancel();

            await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken));

            cancellationToken.ThrowIfCancellationRequested();
        }

        protected abstract Task ExecuteAsync(IServiceProvider scopeServiceProvider,
            CancellationToken cancellationToken);
    }
}