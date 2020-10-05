namespace totlib
{
    public static class DataAccessor
    {
        public static void AppendValues(
            this IDataAccessor dataAccessor,
            string seriesName,
            params string[] values)
        {
            dataAccessor.AppendValues(seriesName, default, values);
        }

        public static void CreateSeries(
            this IDataAccessor dataAccessor,
            string seriesName,
            params string[] columnNames)
        {
            dataAccessor.CreateSeries(seriesName, columnNames);
        }
    }
}