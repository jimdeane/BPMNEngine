
using Microsoft.AspNetCore.Mvc;
using Quartz;
using System.Text.Json;
using WebApp.Jobs;
using WebApp.Services;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BPMNController : ControllerBase
    {
        private readonly BPMNService _bpmnService;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ILogger<BPMNController> _logger;

        public BPMNController(BPMNService bpmnService, ISchedulerFactory schedulerFactory, ILogger<BPMNController> logger)
        {
            _bpmnService = bpmnService;
            _schedulerFactory = schedulerFactory;
            _logger = logger;
        }

        [HttpPost("run")]
        public async Task<IActionResult> RunWorkflow(IFormFile bpmnFile, string variables = "{}")
        {
            try
            {
                if (bpmnFile == null || bpmnFile.Length == 0)
                    return BadRequest("No BPMN file provided");

                using var reader = new StreamReader(bpmnFile.OpenReadStream());
                var bpmnXml = await reader.ReadToEndAsync();

                Dictionary<string, object>? variablesDict = null;
                if (!string.IsNullOrEmpty(variables) && variables != "{}")
                {
                    try
                    {
                        variablesDict = JsonSerializer.Deserialize<Dictionary<string, object>>(variables);
                    }
                    catch (JsonException)
                    {
                        return BadRequest("Invalid JSON format for variables");
                    }
                }

                var result = await _bpmnService.RunWorkflowAsync(bpmnXml, variablesDict);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running workflow");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("schedule")]
        public async Task<IActionResult> ScheduleWorkflow(IFormFile bpmnFile, string cronExpression, string jobName)
        {
            try
            {
                if (bpmnFile == null || bpmnFile.Length == 0)
                    return BadRequest("No BPMN file provided");

                if (string.IsNullOrEmpty(cronExpression))
                    return BadRequest("Cron expression is required");

                if (string.IsNullOrEmpty(jobName))
                    return BadRequest("Job name is required");

                using var reader = new StreamReader(bpmnFile.OpenReadStream());
                var bpmnXml = await reader.ReadToEndAsync();

                // Cache the workflow
                _bpmnService.CacheWorkflow(jobName, bpmnXml);

                var scheduler = await _schedulerFactory.GetScheduler();

                // Create job
                var job = JobBuilder.Create<BPMNWorkflowJob>()
                    .WithIdentity(jobName, "bpmn-workflows")
                    .Build();

                // Create trigger
                var trigger = TriggerBuilder.Create()
                    .WithIdentity($"{jobName}-trigger", "bpmn-workflows")
                    .WithCronSchedule(cronExpression)
                    .Build();

                // Schedule the job
                await scheduler.ScheduleJob(job, trigger);

                return Ok($"Workflow '{jobName}' scheduled successfully with cron expression: {cronExpression}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling workflow");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("jobs")]
        public async Task<IActionResult> GetActiveJobs()
        {
            try
            {
                var scheduler = await _schedulerFactory.GetScheduler();
                var jobGroupNames = await scheduler.GetJobGroupNames();
                var activeJobs = new List<object>();

                foreach (var groupName in jobGroupNames)
                {
                    var jobKeys = await scheduler.GetJobKeys(Quartz.Impl.Matchers.GroupMatcher<JobKey>.GroupEquals(groupName));
                    
                    foreach (var jobKey in jobKeys)
                    {
                        var jobDetail = await scheduler.GetJobDetail(jobKey);
                        var triggers = await scheduler.GetTriggersOfJob(jobKey);
                        
                        foreach (var trigger in triggers)
                        {
                            activeJobs.Add(new
                            {
                                JobName = jobKey.Name,
                                Group = jobKey.Group,
                                Description = jobDetail?.Description,
                                NextFireTime = trigger.GetNextFireTimeUtc()?.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                                PreviousFireTime = trigger.GetPreviousFireTimeUtc()?.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                                TriggerState = await scheduler.GetTriggerState(trigger.Key)
                            });
                        }
                    }
                }

                return Ok(JsonSerializer.Serialize(activeJobs, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active jobs");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpDelete("jobs/{jobName}")]
        public async Task<IActionResult> DeleteJob(string jobName)
        {
            try
            {
                var scheduler = await _schedulerFactory.GetScheduler();
                var jobKey = new JobKey(jobName, "bpmn-workflows");
                
                var deleted = await scheduler.DeleteJob(jobKey);
                
                if (deleted)
                    return Ok($"Job '{jobName}' deleted successfully");
                else
                    return NotFound($"Job '{jobName}' not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
