namespace BigWalkArchipelago.Core.Net
{
    // Lets patches report a check without knowing where it goes.
    internal interface ICheckReporter
    {
        void ReportCheck(string locationId);
    }
}
