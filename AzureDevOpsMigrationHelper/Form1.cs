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
using System.Threading;

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
            browseFolder.Description = "Choose the folder containing all Projects Processes, If does not exist rerun Validation using (/SaveProcesses Switch)";
            if (!string.IsNullOrEmpty(browseFolder.SelectedPath))
            {
                lblFolder.Text = browseFolder.SelectedPath;
            }
        }

        private void BtnForNot_Click(object sender, EventArgs e)
        {
            StreamWriter log = new StreamWriter("logTF402584.txt");
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


                    if (child.Attributes["for"] != null)
                        log.WriteLine($"Fixing error in {file}");
                    child.Attributes.RemoveNamedItem("for");
                    child.Attributes.RemoveNamedItem("not");
                }
                doc.Save(file);
                Thread.Sleep(20);

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
                if (item == "Logs")
                {
                    continue;
                }
                fileConform.Writeline($"{item},{lblFolder.Text}\\{item}");
            }
            fileConform.Flush();
            fileConform.Close();
        }

        private void BtnFIXTF402582(object sender, EventArgs e)
        {
            StreamWriter log = new StreamWriter("logTF402582.txt");
            //OpenFileDialog logfiled = new OpenFileDialog();
            //string logFile;
            //if (logfiled.ShowDialog() == DialogResult.OK)
            //{
            //    logFile = lblLogFile.Text;
            StreamReader sr = new StreamReader(lblLogFile.Text);
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

                foreach (var error in item)
                {




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
                                log.Writeline($"---Processing Project {item.Key}---", MessageType.Info);
                                //log.Writeline($"---Processing WorkItem {error.Workitem} Control {error.CustomControl}---");
                                Console.WriteLine($"Removing Custom Control: {error.CustomControl} in Project {error.Project}", MessageType.Error);
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
            //}




        }

        private void Form1_Load(object sender, EventArgs e)
        {
            OpenFileDialog LogFile = new OpenFileDialog();
            LogFile.Title = $"Choose log file generated by TFS Migration Tool during the validation Process (DataMigrationTool)";
            if (LogFile.ShowDialog() == DialogResult.OK)
            {
                lblLogFile.Text = LogFile.FileName;
            }
            else
            {
                MessageBox.Show("You Must Provide LogFile!", "Oops Exiting Now!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                Application.Exit();
            }

            var result = browseFolder.ShowDialog();
            browseFolder.Description = "Choose the folder containing all Projects Processes, If does not exist rerun Validation using (/SaveProcesses Switch)";
            if (!string.IsNullOrEmpty(browseFolder.SelectedPath))
            {
                lblFolder.Text = browseFolder.SelectedPath;
            }
            else
            {
                MessageBox.Show("You Must Provide Project Processes Folder!", "Oops Exiting Now!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                Application.Exit();
            }



            StreamWriter log = new StreamWriter("Log.txt");
            log.Writeline("---New Session---");
            log.Flush();
            log.Close();
        }

        private void btnFIXTF402539(object sender, EventArgs e)
        {
            StreamWriter log = new StreamWriter("logTF402539.txt");
            //OpenFileDialog logfiled = new OpenFileDialog();
            string logFile;
            //if (logfiled.ShowDialog() == DialogResult.OK)
            //{
            logFile = lblLogFile.Text;
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

                foreach (var error in item)
                {




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
                                log.Writeline($"---Processing Project {item.Key}---", MessageType.Info);
                                log.Writeline($"Removing Field{error.Field} from AssignedTo in Project: {error.Project}", MessageType.Error);
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
            //}

        }

        private void btnCleanLogs(object sender, EventArgs e)
        {
            int TF402584 = 0; int TF402582 = 0; int TF402539 = 0;

            var logfilename = lblLogFile.Text;
            //var sfDialog = new SaveFileDialog();



            StreamReader sr = new StreamReader(logfilename);
            StreamWriter sw = new StreamWriter("ErrorLogs.txt");
            StreamWriter errSW = new StreamWriter("errrorCount.txt", true);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (line.StartsWith("[Error"))
                {
                    if (line.Contains("TF402584"))
                    {
                        TF402584++;
                        btnForNot.Text = btnForNot.Text + $" ({TF402584})";

                    }
                    else if (line.Contains("TF402582"))
                    {
                        TF402582++;
                        btnForNot.Text = btnForNot.Text + $" ({TF402582})";

                    }
                    else if (line.Contains("TF402539"))
                    {
                        TF402539++;
                        btnForNot.Text = btnForNot.Text + $" ({TF402539})";

                    }
                    sw.Writeline(line);



                }



            }
            errSW.Writeline($"TF402584: {TF402584}          TF402582: {TF402582}            TF402539: {TF402539}");
            errSW.Flush();
            errSW.Close();
            sw.Flush();
            sw.Close();


            sr.Close();






        }

        private void btnCLeanEmptyGroups(object sender, EventArgs e)
        {
            StreamWriter log = new StreamWriter("cleanGroups");
            //OpenFileDialog logfiled = new OpenFileDialog();
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
            //OpenFileDialog logfiled = new OpenFileDialog();
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
            ExecuteAllConform("", txtCollectionUrl.Text, "");

            //        //string

            //    }


            //}
        }

        private List<string> GetErroredProject(bool WriteTemplate = true)
        {
            List<string> ErrorProjects = new List<string>();
            StreamWriter sw;
            if (WriteTemplate)
            {
                sw = new StreamWriter("ConformErrorProjects.csv");
                sw.Writeline("Project,ProcessPath", MessageType.Error);
            }
            else
            {
                sw = new StreamWriter("ErrorProjects.csv");
                sw.Writeline("Project", MessageType.Error);
            }


            StreamReader sr = new StreamReader(lblLogFile.Text);
            string line;

            string filepath = "";
            string projectname = "None";
            bool added = false;
            while (!sr.EndOfStream)
            {

                line = sr.ReadLine();
                if (line.Contains("Starting validation of project"))
                {
                    added = false;
                    projectname = (line.Split('=')[1]).Split(',')[0];
                    projectname = projectname.Replace(".", "").Replace("-", "");
                    filepath = lblFolder.Text + "\\" + projectname;
                }
                if (line.Contains("[Error") && projectname != "None" && added == false)
                {
                    if (WriteTemplate)
                    {
                        sw.Writeline($"{projectname},{filepath}", MessageType.Error);
                    }
                    else
                    {
                        sw.Writeline($"{projectname}", MessageType.Error);
                    }

                    ErrorProjects.Add(projectname);
                    added = true;
                }
            }
            sw.Flush();
            sw.Close();
            return ErrorProjects;
        }

        private static void ExecuteAllConform(string ProjectName, string CollectionURL, string templatePath)
        {
            string scriptPath = @"./ConformFromCSV.ps1";
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.FileName = @"powershell.exe";
            proc.Arguments = @"-noexit -command " + @"&'" + scriptPath + $"' '{CollectionURL}' 'conform.csv'";
            proc.WindowStyle = ProcessWindowStyle.Normal;
            Process.Start(proc);
        }
        private static void ExecuteErrorConform(string ProjectName, string CollectionURL, string templatePath)
        {
            string scriptPath = @"./ConformFromCSV.ps1";
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.FileName = @"powershell.exe";
            proc.Arguments = @"-noexit -command " + @"&'" + scriptPath + $"' '{CollectionURL}' 'ConformErrorProjects.csv'";
            proc.WindowStyle = ProcessWindowStyle.Normal;
            Process.Start(proc);
        }
        private static void ExecuteConform1(string ProjectName, string CollectionURL, string templatePath)
        {
            string scriptPath = @"./ConformProject.ps1";
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.FileName = @"powershell.exe";
            proc.Arguments = @"-noexit -command " + @"&'" + scriptPath + $"' '{CollectionURL}' '{ProjectName}' '{templatePath}'";
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
        public class FieldsErrorTF402577
        {
            public string Project { get; set; }
            public string WorkItemFilePath { get; set; }
            public string Field { get; set; }
            public string Name { get; set; }
        }

        private void BtnClearConsole_Click(object sender, EventArgs e)
        {
            Console.Clear();
        }

        private void BtnConformProject_Click(object sender, EventArgs e)
        {
            var projects = Directory.GetDirectories(lblFolder.Text).Select(x => new DirectoryInfo(x).Name).ToList();
            ConformProject frmConformProject = new ConformProject(projects);
            var result = frmConformProject.ShowDialog();
            if (result != DialogResult.OK || frmConformProject.SelectedProject == "None" || !projects.Contains(frmConformProject.SelectedProject))
            {
                MessageBox.Show("You have not Selected a Project to Conform!", "Canceling!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //frmConformProject.Close();
                frmConformProject.Dispose();
            }
            else
            {
                ExecuteConform1(frmConformProject.SelectedProject, txtCollectionUrl.Text, lblFolder.Text + "\\" + frmConformProject.SelectedProject);
                frmConformProject.Close();
            }
        }

        private void BtnConformErrorProjects_Click(object sender, EventArgs e)
        {
            List<string> ErroredProject = GetErroredProject();
            if (!File.Exists("ConformErrorProjects.csv"))
            {
                GetErroredProject();
            }

            Console.WriteLine("Now Will launch Powershell to Import all changes to Azure DevOps Server! Please dont close the powershell Windows !");
            ExecuteErrorConform("", txtCollectionUrl.Text, "");
        }

        private void Button1_Click_1(object sender, EventArgs e)
        {
            GetErroredProject(false);
        }

        private void BtnFixTF402577_Click(object sender, EventArgs e)
        {
            StreamWriter log = new StreamWriter("logTF402577.txt");

            StreamReader sr = new StreamReader(lblLogFile.Text);
            string line;
            List<FieldsErrorTF402577> WIControls = new List<FieldsErrorTF402577>();
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
                if (line.Contains("TF402577"))
                {
                    string filePath = line.Split(new[] { @"Invalid process template: " }, StringSplitOptions.RemoveEmptyEntries)[1].Split(new[] { "xml" }, StringSplitOptions.RemoveEmptyEntries)[0] + "xml";
                    string OriginalLine = line;
                    line = line.Substring(line.IndexOf("TF402577: Field "));
                    line = line.Replace("TF402577: Field ", "");
                    string refField = line.Split(' ')[0].Trim();

                    OriginalLine = OriginalLine.Substring(OriginalLine.IndexOf("specifies friendly name "));
                    OriginalLine = OriginalLine.Replace("specifies friendly name ", "");
                    string name = OriginalLine.Split(' ')[0].Trim();


                    WIControls.Add(new FieldsErrorTF402577()
                    {
                        Project = projectname,
                        WorkItemFilePath = $"{lblFolder.Text}\\{projectname}\\{filePath}",
                        Field = refField,
                        Name = name
                    });
                }
            }
      

            foreach (var item in WIControls)
            {
                XDocument doc = null;
                try
                {
                    doc = XDocument.Load(item.WorkItemFilePath);
                }
                catch (Exception ex)
                {
                    log.Writeline($"---error Loading XML File {item.WorkItemFilePath}: {ex.Message}---");

                }
                if (doc != null)
                {
                    var query = doc.Descendants().Where(el => el.Name == "FIELD").Where(x => x.Attribute("refname").Value == item.Field && x.Attribute("name").Value == item.Name );

                    if (query.Count() > 0)
                    {
                        
                        foreach (var result in query)
                        {
                            log.Writeline($"Project: [{item.Project}] Renameing the Name ({item.Name}) of Field ({item.Field}) to ({item.Field.Replace(".", "")}) to fix Error TF402577 in TypeDefination: {item.WorkItemFilePath}");
                            result.Attribute("name").Value = item.Field.Replace(".", "");
                        }
                 
                        doc.Save(item.WorkItemFilePath);
                    }

                }


            }

            log.Flush();
            log.Close();



        }
    }
}








