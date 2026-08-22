using System.Data;

namespace EmailService.Extensions
{
    public static class DataTableExtensions
    {
        public static bool IsNullOrEmpty(this DataTable dt)
        {
            return dt == null || dt.Rows.Count == 0;
        }
    }
}