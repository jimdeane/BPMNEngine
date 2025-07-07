
using BPMNEngine;
using WebApp.Services;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add BPMN and scheduling services
builder.Services.AddSingleton<BPMNService>();
builder.Services.AddQuartz();
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();
app.MapControllers();

// Serve index.html at root
app.MapGet("/", () => Results.Content("""
<!DOCTYPE html>
<html>
<head>
    <title>BPMN Engine Web App</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; }
        .container { max-width: 800px; margin: 0 auto; }
        .section { margin: 20px 0; padding: 20px; border: 1px solid #ddd; border-radius: 5px; }
        input, select, button { margin: 5px; padding: 8px; }
        button { background-color: #007bff; color: white; border: none; border-radius: 3px; cursor: pointer; }
        button:hover { background-color: #0056b3; }
        .result { margin-top: 10px; padding: 10px; background-color: #f8f9fa; border-radius: 3px; }
    </style>
</head>
<body>
    <div class="container">
        <h1>BPMN Engine Web App</h1>
        
        <div class="section">
            <h2>Load and Run BPMN Workflow</h2>
            <input type="file" id="bpmnFile" accept=".bpmn,.xml" />
            <br/>
            <input type="text" id="processVariables" placeholder="Process variables (JSON)" />
            <br/>
            <button onclick="loadAndRun()">Load and Run</button>
            <div id="runResult" class="result" style="display:none;"></div>
        </div>

        <div class="section">
            <h2>Schedule Workflow</h2>
            <input type="file" id="scheduleBpmnFile" accept=".bpmn,.xml" />
            <br/>
            <input type="text" id="cronExpression" placeholder="Cron expression (e.g., 0 */5 * * * ?)" />
            <br/>
            <input type="text" id="jobName" placeholder="Job name" />
            <br/>
            <button onclick="scheduleWorkflow()">Schedule</button>
            <div id="scheduleResult" class="result" style="display:none;"></div>
        </div>

        <div class="section">
            <h2>Active Jobs</h2>
            <button onclick="getActiveJobs()">Refresh</button>
            <div id="activeJobs" class="result" style="display:none;"></div>
        </div>
    </div>

    <script>
        async function loadAndRun() {
            const fileInput = document.getElementById('bpmnFile');
            const variablesInput = document.getElementById('processVariables');
            const resultDiv = document.getElementById('runResult');
            
            if (!fileInput.files[0]) {
                alert('Please select a BPMN file');
                return;
            }
            
            const formData = new FormData();
            formData.append('bpmnFile', fileInput.files[0]);
            formData.append('variables', variablesInput.value || '{}');
            
            try {
                const response = await fetch('/api/bpmn/run', {
                    method: 'POST',
                    body: formData
                });
                
                const result = await response.text();
                resultDiv.style.display = 'block';
                resultDiv.innerHTML = `<pre>${result}</pre>`;
            } catch (error) {
                resultDiv.style.display = 'block';
                resultDiv.innerHTML = `<span style="color: red;">Error: ${error.message}</span>`;
            }
        }
        
        async function scheduleWorkflow() {
            const fileInput = document.getElementById('scheduleBpmnFile');
            const cronInput = document.getElementById('cronExpression');
            const jobNameInput = document.getElementById('jobName');
            const resultDiv = document.getElementById('scheduleResult');
            
            if (!fileInput.files[0] || !cronInput.value || !jobNameInput.value) {
                alert('Please fill in all fields');
                return;
            }
            
            const formData = new FormData();
            formData.append('bpmnFile', fileInput.files[0]);
            formData.append('cronExpression', cronInput.value);
            formData.append('jobName', jobNameInput.value);
            
            try {
                const response = await fetch('/api/bpmn/schedule', {
                    method: 'POST',
                    body: formData
                });
                
                const result = await response.text();
                resultDiv.style.display = 'block';
                resultDiv.innerHTML = result;
            } catch (error) {
                resultDiv.style.display = 'block';
                resultDiv.innerHTML = `<span style="color: red;">Error: ${error.message}</span>`;
            }
        }
        
        async function getActiveJobs() {
            const resultDiv = document.getElementById('activeJobs');
            
            try {
                const response = await fetch('/api/bpmn/jobs');
                const result = await response.text();
                resultDiv.style.display = 'block';
                resultDiv.innerHTML = `<pre>${result}</pre>`;
            } catch (error) {
                resultDiv.style.display = 'block';
                resultDiv.innerHTML = `<span style="color: red;">Error: ${error.message}</span>`;
            }
        }
    </script>
</body>
</html>
""", "text/html"));

app.Run("http://0.0.0.0:5000");
