using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Azure.AI.Details.Common.CLI;

public class TrafficEventPolicy : PipelinePolicy
{
    public event EventHandler<PipelineRequest> OnRequest;
    public event EventHandler<PipelineResponse> OnResponse;

    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        ProcessNext(message, pipeline, currentIndex);
        FireEvents(message);
    }

    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
        FireEvents(message);
    }

    public void FireEvents(PipelineMessage message)
    {
        if (message?.Request is not null)
        {
            OnRequest?.Invoke(this, message.Request);
        }
        if (message?.Response is not null)
        {
            OnResponse?.Invoke(this, message.Response);
        }
    }
}