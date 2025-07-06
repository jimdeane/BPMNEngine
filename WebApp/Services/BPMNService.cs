
using BPMNEngine;
using BPMNEngine.Interfaces;
using System.Text.Json;
using System.Xml;

namespace WebApp.Services
{
    public class BPMNService
    {
        private readonly ILogger<BPMNService> _logger;
        private readonly Dictionary<string, string> _workflowCache = new();

        public BPMNService(ILogger<BPMNService> logger)
        {
            _logger = logger;
        }

        public async Task<string> RunWorkflowAsync(string bpmnXml, Dictionary<string, object>? variables = null)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(bpmnXml);

                var businessProcess = new BusinessProcess(doc, 
                    logging: new BPMNEngine.DelegateContainers.ProcessLogging()
                    {
                        LogLine = (element, assembly, fileName, lineNumber, level, timestamp, message) =>
                        {
                            _logger.LogInformation($"[{level}] {element?.ID}: {message}");
                        },
                        LogException = (element, assembly, fileName, lineNumber, timestamp, exception) =>
                        {
                            _logger.LogError(exception, $"Error in element {element?.ID}");
                        }
                    });

                var processInstance = businessProcess.BeginProcess(variables);
                
                if (processInstance == null)
                {
                    return "Failed to start process - no valid start event found";
                }

                // Wait for completion with timeout
                var completed = processInstance.WaitForCompletion(TimeSpan.FromMinutes(5));
                
                if (completed)
                {
                    var currentVariables = processInstance.CurrentVariables;
                    var variablesJson = JsonSerializer.Serialize(currentVariables, new JsonSerializerOptions { WriteIndented = true });
                    return $"Process completed successfully.\nFinal variables:\n{variablesJson}";
                }
                else
                {
                    return "Process is still running or suspended";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running BPMN workflow");
                return $"Error: {ex.Message}";
            }
        }

        public void CacheWorkflow(string jobName, string bpmnXml)
        {
            _workflowCache[jobName] = bpmnXml;
        }

        public string? GetCachedWorkflow(string jobName)
        {
            return _workflowCache.TryGetValue(jobName, out var workflow) ? workflow : null;
        }
    }
}
