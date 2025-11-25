namespace Unmeshed.Sdk.Models;

/// <summary>
/// Constants for Unmeshed API values.
/// </summary>
public static class UnmeshedConstants
{
    /// <summary>
    /// Webhook source types.
    /// </summary>
    public static class WebhookSource
    {
        /// <summary>Microsoft Teams webhook source.</summary>
        public const string MsTeams = "MS_TEAMS";
        
        /// <summary>Not defined webhook source.</summary>
        public const string NotDefined = "NOT_DEFINED";
    }

    /// <summary>
    /// Authentication user types.
    /// </summary>
    public static class SQAuthUserType
    {
        /// <summary>Regular user authentication.</summary>
        public const string User = "USER";
        
        /// <summary>API authentication.</summary>
        public const string Api = "API";
        
        /// <summary>Internal authentication.</summary>
        public const string Internal = "INTERNAL";
    }

    /// <summary>
    /// Step types for process execution.
    /// </summary>
    public static class StepType
    {
        /// <summary>Worker step type.</summary>
        public const string Worker = "WORKER";
        
        /// <summary>HTTP step type.</summary>
        public const string Http = "HTTP";
        
        /// <summary>Wait step type.</summary>
        public const string Wait = "WAIT";
        
        /// <summary>Fail step type.</summary>
        public const string Fail = "FAIL";
        
        /// <summary>Python step type.</summary>
        public const string Python = "PYTHON";
        
        /// <summary>JavaScript step type.</summary>
        public const string JavaScript = "JAVASCRIPT";
        
        /// <summary>JQ step type.</summary>
        public const string Jq = "JQ";
        
        /// <summary>Managed step type.</summary>
        public const string Managed = "MANAGED";
        
        /// <summary>Built-in step type.</summary>
        public const string Builtin = "BUILTIN";
        
        /// <summary>No-operation step type.</summary>
        public const string Noop = "NOOP";
        
        /// <summary>Update step type.</summary>
        public const string UpdateStep = "UPDATE_STEP";
        
        /// <summary>Send response step type.</summary>
        public const string SendResponse = "SEND_RESPONSE";
        
        /// <summary>Persisted state step type.</summary>
        public const string PersistedState = "PERSISTED_STATE";
        
        /// <summary>Depends on step type.</summary>
        public const string DependsOn = "DEPENDSON";
        
        /// <summary>GraphQL step type.</summary>
        public const string GraphQL = "GRAPHQL";
        
        /// <summary>Flow gateway step type.</summary>
        public const string FlowGateway = "FLOW_GATEWAY";
        
        /// <summary>Decision engine step type.</summary>
        public const string DecisionEngine = "DECISION_ENGINE";
        
        /// <summary>AI agent step type.</summary>
        public const string AiAgent = "AI_AGENT";
        
        /// <summary>SQLite step type.</summary>
        public const string Sqlite = "SQLITE";
        
        /// <summary>Integration step type.</summary>
        public const string Integration = "INTEGRATION";
        
        /// <summary>Exit step type.</summary>
        public const string Exit = "EXIT";
        
        /// <summary>Sub-process step type.</summary>
        public const string SubProcess = "SUB_PROCESS";
        
        /// <summary>List step type.</summary>
        public const string List = "LIST";
        
        /// <summary>Parallel step type.</summary>
        public const string Parallel = "PARALLEL";
        
        /// <summary>ForEach step type.</summary>
        public const string ForEach = "FOREACH";
        
        /// <summary>While step type.</summary>
        public const string While = "WHILE";
        
        /// <summary>Switch step type.</summary>
        public const string Switch = "SWITCH";
        
        /// <summary>Scheduler step type.</summary>
        public const string Scheduler = "SCHEDULER";
        
        /// <summary>Process tracker step type.</summary>
        public const string ProcessTracker = "PROCESS_TRACKER";
    }

    /// <summary>
    /// Step status values.
    /// </summary>
    public static class StepStatus
    {
        /// <summary>Step is pending.</summary>
        public const string Pending = "PENDING";
        
        /// <summary>Step is scheduled.</summary>
        public const string Scheduled = "SCHEDULED";
        
        /// <summary>Step is running.</summary>
        public const string Running = "RUNNING";
        
        /// <summary>Step is paused.</summary>
        public const string Paused = "PAUSED";
        
        /// <summary>Step is completed.</summary>
        public const string Completed = "COMPLETED";
        
        /// <summary>Step has failed.</summary>
        public const string Failed = "FAILED";
        
        /// <summary>Step has timed out.</summary>
        public const string TimedOut = "TIMED_OUT";
        
        /// <summary>Step was skipped.</summary>
        public const string Skipped = "SKIPPED";
        
        /// <summary>Step was cancelled.</summary>
        public const string Cancelled = "CANCELLED";
    }

    /// <summary>
    /// Process status values.
    /// </summary>
    public static class ProcessStatus
    {
        /// <summary>Process is running.</summary>
        public const string Running = "RUNNING";
        
        /// <summary>Process is completed.</summary>
        public const string Completed = "COMPLETED";
        
        /// <summary>Process has failed.</summary>
        public const string Failed = "FAILED";
        
        /// <summary>Process has timed out.</summary>
        public const string TimedOut = "TIMED_OUT";
        
        /// <summary>Process was cancelled.</summary>
        public const string Cancelled = "CANCELLED";
        
        /// <summary>Process was terminated.</summary>
        public const string Terminated = "TERMINATED";
        
        /// <summary>Process was reviewed.</summary>
        public const string Reviewed = "REVIEWED";
    }

    /// <summary>
    /// Process trigger types.
    /// </summary>
    public static class ProcessTriggerType
    {
        /// <summary>Manual trigger.</summary>
        public const string Manual = "MANUAL";
        
        /// <summary>Scheduled trigger.</summary>
        public const string Scheduled = "SCHEDULED";
        
        /// <summary>API mapping trigger.</summary>
        public const string ApiMapping = "API_MAPPING";
        
        /// <summary>Webhook trigger.</summary>
        public const string Webhook = "WEBHOOK";
        
        /// <summary>API trigger.</summary>
        public const string Api = "API";
        
        /// <summary>Step trigger.</summary>
        public const string Step = "STEP";
        
        /// <summary>Integration consumer trigger.</summary>
        public const string IntegrationConsumer = "INTEGRATION_CONSUMER";
        
        /// <summary>Sub-process trigger.</summary>
        public const string SubProcess = "SUB_PROCESS";
        
        /// <summary>Unmeshed MCP tool trigger.</summary>
        public const string UnmeshedMcpTool = "UNMESHED_MCP_TOOL";
    }

    /// <summary>
    /// Process types.
    /// </summary>
    public static class ProcessType
    {
        /// <summary>Standard process type.</summary>
        public const string Standard = "STANDARD";
        
        /// <summary>Dynamic process type.</summary>
        public const string Dynamic = "DYNAMIC";
        
        /// <summary>API orchestration process type.</summary>
        public const string ApiOrchestration = "API_ORCHESTRATION";
        
        /// <summary>Internal process type.</summary>
        public const string Internal = "INTERNAL";
        
        /// <summary>Test process type.</summary>
        public const string Test = "TEST";
    }
}
