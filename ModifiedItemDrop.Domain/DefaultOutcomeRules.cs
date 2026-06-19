namespace FFEmqo.ModifiedItemDrop.Domain
{
    public static class DefaultOutcomeRules
    {
        public const string Xml =
            "<OutcomeRules>" +
            "  <Rule name=\"Default keep\" priority=\"0\">" +
            "    <Target kind=\"Any\" />" +
            "    <Outcome kind=\"Keep\" />" +
            "  </Rule>" +
            "</OutcomeRules>";
    }
}
