using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Extensibility;
using ESRI.ArcGIS.Client.Tasks;
using System.Diagnostics;

namespace Maximo.AddIns
{
    public partial class MaximoDialog : UserControl
    {
        public Map Map { get; internal set; }
        public string serviceUrl { get; internal set; }
        List<Feat> featList = new List<Feat>();
        List<Data> dgSource = new List<Data>();
        private Dictionary<string, Graphic> towers = new Dictionary<string, Graphic>();
        Dictionary<string, int> cntTable = new Dictionary<string, int>();
        string tableUrl = "";
        int rowIdx = 0;
        int wpTotal = 0;
        int ipTotal = 0;
        int bothTotal = 0;


        public MaximoDialog()
        {
            InitializeComponent();

            dataGrid1.LoadingRow += myDataGrid_LoadingRow;
        }

        void myDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            Data d = e.Row.DataContext as Data;
            if (d.Section == "UT/EHV" || d.Section == "TLM" || d.Section == "TLM-O&R" || d.Section == "Total Work Orders")
            {
                //e.Row.Background = new SolidColorBrush(Colors.LightGray);
                e.Row.FontWeight = FontWeights.Bold;
            }
            else if (d.Section == "High Priority Total" || d.Section == "Low Priority Total")
            {
                e.Row.FontWeight = FontWeights.Bold;
                e.Row.Foreground = new SolidColorBrush(Colors.Red);
            }
            rowIdx++;
        }

        private void populateStore(string reqType)
        {
           
            if (reqType == "underground")
            {
                string[] abovePriorities = { "1", "2", "3", "4", "5", "6", "7", "9" };
                string[] aboveSections = { "High/Immediate Action Reqd, (i.e. Cat 1/Cat 2/ENV/Safety/SEC)", "Outage Required", "CM- corrective mtce- Large work scope", "All repetitive work such as Compliances and PMs", "CM- Corrective mtce- small work scope- minor in nature", "CM contractor support required", "WORK FOR OTHERS", "Not Prioritized" };

                Data data = new Data();
                data.Section = "UT/EHV";
                dgSource.Add(data);

                int sumWPCnt = 0;
                int sumIPCnt = 0;
                int sumTotal = 0;
                int idx = 0;
                foreach (string p in abovePriorities)
                {
                    data = new Data();
                    data.Priority = p;
                    data.Section = aboveSections[idx];
                    int wpCnt = 0;
                    int ipCnt = 0;
                    int total = 0;
                    if (cntTable.ContainsKey(p + "WAPPR"))
                        wpCnt = cntTable[p + "WAPPR"];
                    if (cntTable.ContainsKey(p + "INPRG"))
                        ipCnt = cntTable[p + "INPRG"];

                    total = wpCnt + ipCnt;

                    wpTotal += wpCnt;
                    ipTotal += ipCnt;
                    bothTotal += total;
                    data.wpCnt = wpCnt.ToString();
                    data.ipCnt = ipCnt.ToString();
                    data.total = total.ToString();
                    if (wpCnt != 0 || ipCnt != 0)
                        dgSource.Add(data);
                    sumWPCnt += wpCnt;
                    sumIPCnt += ipCnt;
                    sumTotal += total;
                    if (p == "3" && (sumWPCnt != 0 || sumIPCnt != 0 || sumTotal != 0))
                    {
                        data = new Data();
                        data.Section = "High Priority Total";
                        data.wpCnt = sumWPCnt.ToString();
                        data.ipCnt = sumIPCnt.ToString();
                        data.total = sumTotal.ToString();
                        dgSource.Add(data);
                        sumWPCnt = 0;
                        sumIPCnt = 0;
                        sumTotal = 0;
                    }

                    if (p == "9" && (sumWPCnt != 0 || sumIPCnt != 0 || sumTotal != 0))
                    {
                        data = new Data();
                        data.Section = "Low Priority Total";
                        data.wpCnt = sumWPCnt.ToString();
                        data.ipCnt = sumIPCnt.ToString();
                        data.total = sumTotal.ToString();
                        dgSource.Add(data);
                        sumWPCnt = 0;
                        sumIPCnt = 0;
                        sumTotal = 0;
                    }
                    idx++;
                }
            }
            else if (reqType == "above")
            {
                string[] underPriorities = { "1", "2", "3", "4", "5", "9" };
                string[] underSections = { "High/Immediate Action Reqd, (i.e. Cat 1/Cat 2/ENV/Safety/SEC)", "Work Requiring Completion within 7 Days", "Completion Within 30 Days (Summer Load Constraints and CMSEA)", "All repetitive work such as Compliances and PMs", "Non-Critical, Follow-up or Non-Emergency Repair/Maint with no Time Constraints", "Not Prioritized" };

                Data data = new Data();
                data.Section = "TLM";
                dgSource.Add(data);

                int sumWPCnt = 0;
                int sumIPCnt = 0;
                int sumTotal = 0;
                int idx = 0;
                foreach (string p in underPriorities)
                {
                    data = new Data();
                    data.Priority = p;
                    data.Section = underSections[idx];
                    int wpCnt = 0;
                    int ipCnt = 0;
                    int total = 0;
                    if (cntTable.ContainsKey(p + "WAPPR"))
                        wpCnt = cntTable[p + "WAPPR"];
                    if (cntTable.ContainsKey(p + "INPRG"))
                        ipCnt = cntTable[p + "INPRG"];

                    total = wpCnt + ipCnt;

                    wpTotal += wpCnt;
                    ipTotal += ipCnt;
                    bothTotal += total;

                    data.wpCnt = wpCnt.ToString();
                    data.ipCnt = ipCnt.ToString();
                    data.total = total.ToString();
                    if (wpCnt != 0 || ipCnt != 0)
                        dgSource.Add(data);
                    sumWPCnt += wpCnt;
                    sumIPCnt += ipCnt;
                    sumTotal += total;
                    if (p == "3" && (sumWPCnt != 0 || sumIPCnt != 0 || sumTotal != 0))
                    {
                        data = new Data();
                        data.Section = "High Priority Total";
                        data.wpCnt = sumWPCnt.ToString();
                        data.ipCnt = sumIPCnt.ToString();
                        data.total = sumTotal.ToString();
                        dgSource.Add(data);
                        sumWPCnt = 0;
                        sumIPCnt = 0;
                        sumTotal = 0;
                    }

                    if (p == "9" && (sumWPCnt != 0 || sumIPCnt != 0 || sumTotal != 0))
                    {
                        data = new Data();
                        data.Section = "Low Priority Total";
                        data.wpCnt = sumWPCnt.ToString();
                        data.ipCnt = sumIPCnt.ToString();
                        data.total = sumTotal.ToString();
                        dgSource.Add(data);
                        sumWPCnt = 0;
                        sumIPCnt = 0;
                        sumTotal = 0;
                    }
                    idx++;
                }
            }
            else if (reqType == "O&R")
            {
                string[] underPriorities = { "1", "2", "3", "4", "5", "9" };
                string[] underSections = { "High/Immediate Action Reqd, (i.e. Cat 1/Cat 2/ENV/Safety/SEC)", "Work Requiring Completion within 7 Days", "Completion Within 30 Days (Summer Load Constraints and CMSEA)", "All repetitive work such as Compliances and PMs", "Non-Critical, Follow-up or Non-Emergency Repair/Maint with no Time Constraints", "Not Prioritized" };

                Data data = new Data();
                data.Section = "TLM-O&R";
                dgSource.Add(data);

                int sumWPCnt = 0;
                int sumIPCnt = 0;
                int sumTotal = 0;
                int idx = 0;
                foreach (string p in underPriorities)
                {
                    data = new Data();
                    data.Priority = p;
                    data.Section = underSections[idx];
                    int wpCnt = 0;
                    int ipCnt = 0;
                    int total = 0;
                    if (cntTable.ContainsKey(p + "WAPPR"))
                        wpCnt = cntTable[p + "WAPPR"];
                    if (cntTable.ContainsKey(p + "INPRG"))
                        ipCnt = cntTable[p + "INPRG"];

                    total = wpCnt + ipCnt;

                    wpTotal += wpCnt;
                    ipTotal += ipCnt;
                    bothTotal += total;

                    data.wpCnt = wpCnt.ToString();
                    data.ipCnt = ipCnt.ToString();
                    data.total = total.ToString();
                    if (wpCnt != 0 || ipCnt != 0)
                        dgSource.Add(data);
                    sumWPCnt += wpCnt;
                    sumIPCnt += ipCnt;
                    sumTotal += total;
                    if (p == "3" && (sumWPCnt != 0 || sumIPCnt != 0 || sumTotal != 0))
                    {
                        data = new Data();
                        data.Section = "High Priority Total";
                        data.wpCnt = sumWPCnt.ToString();
                        data.ipCnt = sumIPCnt.ToString();
                        data.total = sumTotal.ToString();
                        dgSource.Add(data);
                        sumWPCnt = 0;
                        sumIPCnt = 0;
                        sumTotal = 0;
                    }

                    if (p == "9" && (sumWPCnt != 0 || sumIPCnt != 0 || sumTotal != 0))
                    {
                        data = new Data();
                        data.Section = "Low Priority Total";
                        data.wpCnt = sumWPCnt.ToString();
                        data.ipCnt = sumIPCnt.ToString();
                        data.total = sumTotal.ToString();
                        dgSource.Add(data);
                        sumWPCnt = 0;
                        sumIPCnt = 0;
                        sumTotal = 0;
                    }
                    idx++;
                }
                Data dataTotal = new Data();
                dataTotal.Section = "Total Work Orders";
                dataTotal.wpCnt = wpTotal.ToString();
                dataTotal.ipCnt = ipTotal.ToString();
                dataTotal.total = bothTotal.ToString();
                dgSource.Add(dataTotal);
                dataGrid1.ItemsSource = dgSource;
            }
        }

        public void loadData(string reqType="")
        {
            QueryTask queryTask;
            queryTask = new QueryTask(serviceUrl);

            queryTask.ExecuteCompleted += QueryTask_ExecuteCompleted;
            queryTask.Failed += QueryTask_Failed;

            ESRI.ArcGIS.Client.Tasks.Query query = new ESRI.ArcGIS.Client.Tasks.Query();
            // Specify fields to return from initial query
            query.OutFields.AddRange(new string[] { "Status", "Priority" });

            // This query will just populate the drop-down, so no need to return geometry
            query.ReturnGeometry = false;

            // Return all features
            if (reqType == "")
            {
                query.Where = "Department='TO'";
                queryTask.ExecuteAsync(query, "underground");
            }
            else if (reqType == "above") {
                query.Where = "Department='TLM' and Section<>'TLM-O&R (EHV)'";
                queryTask.ExecuteAsync(query, "above");
            }
            else if (reqType == "O&R")
            {
                query.Where = "Department='TLM' and Section='TLM-O&R (EHV)'";
                queryTask.ExecuteAsync(query, "O&R");
            }
        }

        private void QueryTask_ExecuteCompleted(object sender, ESRI.ArcGIS.Client.Tasks.QueryEventArgs args)
        {
            FeatureSet featureSet = args.FeatureSet;
            featList = new List<Feat>();
            foreach (Graphic graphic in args.FeatureSet.Features)
            {
                Feat f = new Feat();
                f.Priority = graphic.Attributes["Priority"].ToString();
                f.Status = graphic.Attributes["Status"].ToString();
                featList.Add(f);
            }

            var priorityGroups =
            from order in featList
            orderby order.Priority, order.Status // orderby not necessary, but neater
            group order by new { order.Priority, order.Status };

            cntTable = new Dictionary<string, int>();
            foreach (var priorityGroup in priorityGroups)
            {
                Debug.WriteLine("Priority {0} has status {1} for a total count of {2}",
                    priorityGroup.Key.Priority,
                    priorityGroup.Key.Status,
                    priorityGroup.Count());

                cntTable.Add(priorityGroup.Key.Priority + priorityGroup.Key.Status, priorityGroup.Count());
            }

            if ((args.UserState as string) == "underground")
            {
                populateStore("underground");
                loadData("above");
            }
            else if ((args.UserState as string) == "above")
            {
                populateStore("above");
                loadData("O&R");
            }
            else if ((args.UserState as string) == "O&R")
            {
                populateStore("O&R");
            }

            Debug.WriteLine("Done");
        }

        private void QueryTask_Failed(object sender, TaskFailedEventArgs args)
        {
            MessageBox.Show("Query failed: " + args.Error);
        }
    }

    public class Feat
    {
        public string Priority { get; set; }
        public string Status { get; set; }
    }

    public class Data
    {
        public string Priority { get; set; }
        public string Section { get; set; }
        public string wpCnt { get; set; }
        public string ipCnt { get; set; }
        public string total { get; set; }
    }

}
