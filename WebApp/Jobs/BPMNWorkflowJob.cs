
using Quartz;
using WebApp.Services;

namespace WebApp.Jobs
{
    public class BPMNWorkflowJob : IJob
    {
        private readonly BPMNService _bpmnService;
        private readonly ILogger<BPMNWorkflowJob> _logger;

        public BPMNWorkflowJob(BPMNService bpmnService, ILogger<BPMNWorkflowJob> logger)
        {
            _bpmnService = bpmnService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var jobName = context.JobDetail.Key.Name;
            
            try
            {
                _logger.LogInformation($"Starting scheduled BPMN workflow: {jobName}");
                
                var bpmnXml = _bpmnService.GetCachedWorkflow(jobName);
                if (string.IsNullOrEmpty(bpmnXml))
                {
                    _logger.LogError($"No cached workflow found for job: {jobName}");
                    return;
                }

                var result = await _bpmnService.RunWorkflowAsync(bpmnXml);
                _logger.LogInformation($"Workflow {jobName} completed: {result}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing scheduled workflow: {jobName}");
                throw;
            }
        }
    }
}
