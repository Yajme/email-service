using System;
using DevExpress.XtraReports.UI;
using DevExpress.XtraPrinting;
using System.IO;
using System.Diagnostics;
using System.Data;
using EmailService.Extensions;

namespace EmailService.DevExpress
{
    public class ReportHelper
    {

        public string FilePath { get;  } = string.Empty;
        private DataTable TemplateData { get;  } = new DataTable();

        public ReportHelper(string _filePath)
        {
            FilePath = _filePath;
        }

        public ReportHelper(string _filePath, DataTable data)
        {
            FilePath = _filePath;
            TemplateData = data?.Copy() ?? new DataTable();
        }


        public string ReportToHtml()
        {
            try
            {
                Trace.TraceInformation("Converting the repx file to HTML");
                string HtmlContent = string.Empty;
                
                if(!FIle.Exists(FilePath))
                {
                    throw new FileNotFoundException($"File not found: {FilePath}");
                }
                
                XtraReport report = XtraReport.FromFile(FilePath, true);

                report.CreateDocument();


                if(!TemplateData.IsNullOrEmpty())
                {
                    report.DataSource = TemplateData;
                    report.DataMember = TemplateData.TableName;
                }

                using (var htmlStream = new MemoryStream())
                {
                    report.ExportToHtml(htmlStream, new HtmlExportOptions
                    {
                        EmbedImagesInHTML = true,
                        InlineCss = true
                    });

                    htmlStream.Seek(0, SeekOrigin.Begin);
                    HtmlContent = new StreamReader(htmlStream).ReadToEnd();
                }
                Trace.TraceInformation("repx file converted to html");
                return HtmlContent;
            }
            catch(FileNotFoundException ex)
            {
                throw new FileNotFoundException($"File not found: {FilePath}",ex);
            }
            catch(Exception ex)
            {
                throw new Exception($"Something went wrong with file conversion: {ex.Message}", ex);
            }
        }
    }
}
