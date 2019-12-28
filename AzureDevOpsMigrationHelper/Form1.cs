using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Management.Automation;
using System.Diagnostics;

namespace DevOps_Migration_Helper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            var result = browseFolder.ShowDialog();
            if (!string.IsNullOrEmpty(browseFolder.SelectedPath))
            {
                lblFolder.Text = browseFolder.SelectedPath;
            }
        }

        private void BtnForNot_Click(object sender, EventArgs e)
        {
            List<string> files = new List<string>();
            List<string> directory = new List<string>();
            if (txtFiles.Text.Contains(','))
            {
                files = txtFiles.Text.Split(',').ToList();
                foreach (var item in files)
                {
                    directory.AddRange(Directory.GetFiles(lblFolder.Text, "*", SearchOption.AllDirectories).Where(x => x.EndsWith(item)).ToList());
                }
            }
            else if (txtFiles.Text == "*")
            {
                directory = Directory.GetFiles(lblFolder.Text, "*", SearchOption.AllDirectories).Where(x => x.EndsWith(".xml")).ToList();
            }
            else
            {
                directory = Directory.GetFiles(lblFolder.Text, "*", SearchOption.AllDirectories).Where(x => x.EndsWith(txtFiles.Text)).ToList();
            }

            foreach (var file in directory)
            {
                var doc = new System.Xml.XmlDocument();
                doc.Load(file);
                var root = doc.FirstChild;
                foreach (System.Xml.XmlNode child in doc.GetElementsByTagName("TRANSITION"))
                {
                    //if (child.Attributes["for"] != null)
                    //    child.Attributes.Remove(child.Attributes["for"]);
                    child.Attributes.RemoveNamedItem("for");
                    child.Attributes.RemoveNamedItem("not");
                }
                doc.Save(file);
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            CreateConfromFile();

        }

        private void CreateConfromFile()
        {
            var folder = lblFolder.Text;
            var projects = Directory.GetDirectories(folder).Select(x => new DirectoryInfo(x).Name);


            StreamWriter fileConform = new StreamWriter("conform.csv");
            fileConform.AutoFlush = true;
            fileConform.Writeline("Project,ProcessPath");
            foreach (var item in projects)
            {
                fileConform.Writeline($"{item},{lblFolder.Text}\\{item}");
            }
            fileConform.Flush();
            fileConform.Close();
        }

        private void BtnFIXTF402582(object sender, EventArgs e)
        {
            StreamWriter log = new StreamWriter("log.txt");
            OpenFileDialog logfiled = new OpenFileDialog();
            string logFile;
            if (logfiled.ShowDialog() == DialogResult.OK)
            {
                logFile = logfiled.FileName;
                StreamReader sr = new StreamReader(logFile);
                string line;
                List<WIControl> WIControls = new List<WIControl>();
                string filepath = "";
                string projectname = "";
                while (!sr.EndOfStream)
                {

                    line = sr.ReadLine();
                    if (line.Contains("Starting validation of project"))
                    {
                        projectname = (line.Split('=')[1]).Split(',')[0];
                        projectname = projectname.Replace(".", "").Replace("-", "");
                        filepath = lblFolder.Text + "\\" + projectname;
                    }
                    if (line.Contains("TF402582"))
                    {
                        line = line.Substring(line.IndexOf("Work item type "));
                        line = line.Replace("Work item type", "");
                        line = line.Replace("contains custom control ", ",");
                        line = line.Replace(" which is not supported.", "");
                        line = line.Trim();


                        WIControls.Add(new WIControl()
                        {
                            Project = projectname,
                            FilePath = filepath,
                            Workitem = line.Split(',')[0].Trim(),
                            CustomControl = line.Split(',')[1]
                        });
                    }
                }

                //List<string> files = new List<string>();
                //foreach (var item in WIControls)
                //{
                //    directory = Directory.GetFiles(lblFolder.Text, "*", SearchOption.AllDirectories).Where(x => x.EndsWith(".xml")).ToList();
                //}

                var ProjectGroups = WIControls.GroupBy(x => x.Project);
                foreach (var item in ProjectGroups)
                {
                    log.Writeline($"---Processing Project {item.Key}---");
                    foreach (var error in item)
                    {

                        log.Writeline($"---Processing WorkItem {error.Workitem} Control {error.CustomControl}---");


                        var filename = $"{error.FilePath}\\WorkItem Tracking\\TypeDefinitions\\{ error.Workitem.Replace(" ", "")}.xml";
                        XDocument doc = null;
                        try
                        {
                            doc = XDocument.Load(filename);
                        }
                        catch (Exception ex)
                        {
                            log.Writeline($"---error Loading XML File {filename}: {ex.Message}---");

                        }

                        //XElement all = XElement.Parse(doc.InnerXml);
                        //var qry = from element in doc.Descendants()
                        //          where (string)element.Attribute("Type") == error.CustomControl && element.Name.LocalName == "Control"
                        //          select element;
                        if (doc != null)
                        {
                            var query = doc.Descendants().Where(el => el.Name == "Control").Where(x => x.Attribute("Type").Value == error.CustomControl);

                            if (query.Count() > 0)
                            {
                                List<XElement> BeRemove = new List<XElement>();
                                foreach (var result in query)
                                {
                                    var groupparent = result.Parent;
                                    if (groupparent.Nodes().Count() == 1)
                                    {
                                        BeRemove.Add(groupparent);
                                    }
                                    else
                                    {
                                        BeRemove.Add(result);
                                    }
                                }
                                foreach (var rem in BeRemove)
                                {
                                    rem.Remove();
                                }
                                doc.Save(filename);
                            }

                        }
                        log.Writeline("End");

                        log.Flush();

                    }

                }
                log.Close();
            }




        }

        private void Form1_Load(object sender, EventArgs e)
        {
            StreamWriter log = new StreamWriter("Log.txt");
            log.Writeline("---New Session---");
            log.Flush();
            log.Close();
        }

        private void btnFIXTF402539(object sender, EventArgs e)
        {
            StreamWriter log = new StreamWriter("logTF402539.txt");
            OpenFileDialog logfiled = new OpenFileDialog();
            string logFile;
            if (logfiled.ShowDialog() == DialogResult.OK)
            {
                logFile = logfiled.FileName;
                StreamReader sr = new StreamReader(logFile);
                string line;
                List<FieldsError> FieldsErrors = new List<FieldsError>();
                string filepath = "";
                string projectname = "";
                while (!sr.EndOfStream)
                {

                    line = sr.ReadLine();
                    if (line.Contains("Starting validation of project"))
                    {
                        projectname = (line.Split('=')[1]).Split(',')[0];
                        projectname = projectname.Replace(".", "").Replace("-", "");
                        filepath = lblFolder.Text + "\\" + projectname;
                    }
                    if (line.Contains("TF402539"))
                    {
                        line = line.Substring(line.IndexOf("TypeDefinitions"));
                        line = line.Replace("TypeDefinitions\\", "");
                        var workitemname = line.Split(':')[0].Replace(".xml", "");

                        line = line.Substring(line.IndexOf("TF402539: "));
                        line = line.Replace("TF402539: ", "");

                        line = line.Replace("Field ", "");

                        line = line.Trim();


                        FieldsErrors.Add(new FieldsError()
                        {
                            Project = projectname,
                            FilePath = filepath,
                            Workitem = workitemname,
                            Field = line.Split(' ')[0]

                        });
                    }
                }

                //-------------------------------------------

                var ProjectGroups = FieldsErrors.GroupBy(x => x.Project);
                foreach (var item in ProjectGroups)
                {
                    log.Writeline($"---Processing Project {item.Key}---");
                    foreach (var error in item)
                    {

                        log.Writeline($"---Processing WorkItem {error.Workitem} Field {error.Field}---");


                        var filename = $"{error.FilePath}\\WorkItem Tracking\\TypeDefinitions\\{ error.Workitem.Replace(" ", "")}.xml";
                        XDocument doc = null;
                        try
                        {
                            doc = XDocument.Load(filename);
                        }
                        catch (Exception ex)
                        {
                            log.Writeline($"---error Loading XML File {filename}: {ex.Message}---");

                        }
                        if (doc != null)
                        {
                            var query = doc.Descendants().Where(el => el.Name == "FIELD").Where(x => x.Attribute("refname").Value == error.Field);

                            if (query.Count() > 0)
                            {
                                List<XElement> BeRemove = new List<XElement>();
                                foreach (var result in query)
                                {
                                    BeRemove = result.Descendants().Where(x => x.Parent == result && x.Name != "HELPTEXT" && x.Name != "ALLOWEXISTINGVALUE" && x.Name != "VALIDUSER" && x.Name != "REQUIRED").ToList();
                                }
                                foreach (var rem in BeRemove)
                                {
                                    rem.Remove();
                                }
                                doc.Save(filename);
                            }

                        }
                        log.Writeline("End");

                        log.Flush();

                    }

                }
                log.Close();
            }

        }

        private void btnCleanLogs(object sender, EventArgs e)
        {
            var ofDialog = new OpenFileDialog();

            if (ofDialog.ShowDialog() == DialogResult.OK)
            {
                var logfilename = ofDialog.FileName;
                var sfDialog = new SaveFileDialog();

                if (sfDialog.ShowDialog() == DialogResult.OK)
                {
                    var savefile = sfDialog.FileName;
                    StreamReader sr = new StreamReader(logfilename);
                    StreamWriter sw = new StreamWriter(savefile);
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (line.StartsWith("[Error"))
                        {
                            sw.Writeline(line);
                        }
                    }
                }


            }



        }

        private void btnCLeanEmptyGroups(object sender, EventArgs e)
        {
            StreamWriter log = new StreamWriter("cleanGroups");
            OpenFileDialog logfiled = new OpenFileDialog();
            var Projects = Directory.GetDirectories(lblFolder.Text).Select(x => new DirectoryInfo(x).Name);
            log.Writeline($"---Start Cleaning---");
            foreach (var item in Projects)
            {
                if (item == "Logs")
                {
                    continue;
                }
                log.Writeline($"---Processing Project {item}");
                var workitems = Directory.GetFiles($"{lblFolder.Text}\\{item}\\WorkItem Tracking\\TypeDefinitions");
                foreach (var workitemPath in workitems)
                {
                    log.Writeline($"---Processing Workitem {workitemPath}---");
                    XDocument doc = null;
                    try
                    {
                        doc = XDocument.Load(workitemPath);
                    }
                    catch (Exception ex)
                    {
                        log.Writeline($"---error Loading XML File {workitemPath}: {ex.Message}---");
                    }
                    var query = doc.Descendants().Where(el => el.Name == "Group").Where(x => x.Descendants().Count() == 0);
                    if (query.Count() > 0)
                    {
                        var ToBeremoved = new List<XElement>();
                        log.Writeline($"----------------------------------------------removed {query.Count()} Groups---");
                        foreach (var result in query)
                        {
                            ToBeremoved.Add(result);
                        }
                        foreach (var remove in ToBeremoved)
                        {
                            remove.Remove();
                        }

                        doc.Save(workitemPath);
                    }


                }
            }

            log.Writeline("End");

            log.Flush();


            log.Close();
        }

        private void btnGetProjectsWithBugs(object sender, EventArgs e)
        {
            var folder = lblFolder.Text;
            var projects = Directory.GetDirectories(folder).Select(x => new DirectoryInfo(x).Name);


            StreamWriter fileProject = new StreamWriter("Projects.csv");
            fileProject.AutoFlush = true;
            foreach (var item in projects)
            {
                if (item == "Logs")
                {
                    continue;
                }
                fileProject.Writeline($"{item}");
            }
            fileProject.Flush();
            fileProject.Close();
        }

        private void btnFixBugsWorkitems(object sender, EventArgs e)
        {
            StreamWriter log = new StreamWriter("BugWorkItemsProjectsLog.txt");
            OpenFileDialog logfiled = new OpenFileDialog();
            var projects = Directory.GetDirectories(lblFolder.Text).Select(x => new DirectoryInfo(x).Name);
            foreach (var item in projects)
            {
                if (item == "Logs")
                {
                    continue;
                }
                var line = item;

                log.Writeline($"---Processing Project {line}");
                var ProjectProcessConfiguration = $"{lblFolder.Text}\\{line}\\WorkItem Tracking\\Process\\ProcessConfiguration.xml";
                XDocument doc = null;
                try
                {
                    doc = XDocument.Load(ProjectProcessConfiguration);
                }
                catch (Exception ex)
                {
                    log.Writeline($"---error Loading XML File {ProjectProcessConfiguration}: {ex.Message}---");
                }
                var query = doc.Descendants().Where(el => el.Name == "BugWorkItems");

                if (query.Count() > 0)
                {
                    var states = query.First().Descendants().Where(x => x.Name == "States");
                    //<State value="New" type="Proposed" />
                    if (states.Descendants().ToList().Where(a => a.Attributes().Where(atr => atr.Name == "value" && atr.Value == "New").Count() > 0).Count() == 0)
                    {
                        log.Writeline($"-------------Fixing {line}");
                        XElement NewState = new XElement("State");
                        XAttribute attrValue = new XAttribute("value", "New");
                        XAttribute attrType = new XAttribute("type", "Proposed");
                        NewState.Add(attrValue);
                        NewState.Add(attrType);
                        states.FirstOrDefault().Add(NewState);
                        doc.Save(ProjectProcessConfiguration);
                    }
                }

            }

        }





        private void btnConformProjects_Click(object sender, EventArgs e)
        {
            if (!File.Exists("conform.csv"))
            {
                CreateConfromFile();
            }
            //string Projects;
            //OpenFileDialog dia = new OpenFileDialog();
            //if (dia.ShowDialog() == DialogResult.OK)
            //{
            //    Projects = dia.FileName;
            //    StreamReader sr = new StreamReader(Projects);
            //    sr.ReadLine();
            //    string project;
            //    string process;
            //    while (!sr.EndOfStream)
            //    {
            //        var line = sr.ReadLine();
            //        project = line.Split(',')[0];
            //        process = line.Split(',')[1];
            //        Console.WriteLine($"Importing project {project}.....");
            Console.WriteLine("Now Will launch Powershell to Import all changes to Azure DevOps Server! Please dont close the powershell Windows !");
            ExecuteConform2("", txtCollectionUrl.Text, "");

            //        //string

            //    }


            //}
        }

        private static void ExecuteConform2(string ProjectName, string CollectionURL, string templatePath)
        {
            string scriptPath = @"./ConformFromCSV.ps1";
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.FileName = @"powershell.exe";
            proc.Arguments = @"-noexit -command " + @"&'" + scriptPath + $"' '{CollectionURL}' 'conform.csv'";
            proc.WindowStyle = ProcessWindowStyle.Normal;
            Process.Start(proc);
        }
        private static void ExecuteConform(string ProjectName, string CollectionURL, string templatePath)
        {
            RunspaceConfiguration runspaceConfiguration = RunspaceConfiguration.Create();

            Runspace runspace = RunspaceFactory.CreateRunspace(runspaceConfiguration);
            runspace.Open();

            RunspaceInvoke scriptInvoker = new RunspaceInvoke(runspace);

            Pipeline pipeline = runspace.CreatePipeline();
            pipeline.Commands.AddScript("Set-ExecutionPolicy -Scope Process -ExecutionPolicy Unrestricted");
            pipeline.Commands.AddScript($".\\ConformFromCSV.ps1 {CollectionURL} 'conform.csv'");
            //Here's how you add a new script with arguments
            //Command myCommand = new Command("./conform.ps1");

            //myCommand.Parameters.Add(new CommandParameter("ProjectName", ProjectName));
            //myCommand.Parameters.Add(new CommandParameter("CollectionURL", CollectionURL));
            //myCommand.Parameters.Add(new CommandParameter("templatePath", templatePath));

            //pipeline.Commands.Add(myCommand);

            // Execute PowerShell script
            pipeline.Invoke();
        }

        public class WIControl
        {
            public string Project { get; set; }
            public string FilePath { get; set; }
            public string Workitem { get; set; }
            public string CustomControl { get; set; }
        }

        public class FieldsError
        {
            public string Project { get; set; }
            public string FilePath { get; set; }
            public string Field { get; set; }
            public string Workitem { get; set; }
        }
    }
}








