// By: Jeremiah Stillings
// Narogg - Xegony Server
// Narogg's Heal Parser 2.0
// jstillings1@outlook.com
// 6-27-17

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Data.SqlClient;

namespace HealParser
{
    public partial class Form1 : Form
    {
        //Parser variables
        int _logLoadTimer = 0;
        private int _healCount;
        private int _playerHealAmt;
        private int _totalHealAmt;
        private string _playerHeal;
        private string _encounterName = null;
        private string _healee;
        private string _spellNamefinal;
        private string _spellPlayerName;
        private string _path;
        private string _userView1;
        private string _userView2;
        private string _mob;
        private bool _currentMobDead;
        private string _currentMob;
        private string fixedline2;
        AlertForm _alert;
        string _connetionString;
        SqlConnection _connection;
        DataSet _ds = new DataSet();
        string _sql;
        string _sql2;
        Stats _stats;
        //end parser variables


        public Form1()
        {
            InitializeComponent();
            //mandatory. Otherwise will throw an exception when calling ReportProgress method  
            backgroundWorker1.WorkerReportsProgress = true;

            //mandatory. Otherwise we would get an InvalidOperationException when trying to cancel the operation  
            backgroundWorker1.WorkerSupportsCancellation = true;
        }

        // opens the file selector and will start the background task of parsing

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult eqlogfile = openFileDialog1.ShowDialog();
            _path = openFileDialog1.FileName;

            switch (eqlogfile)
            {
                case DialogResult.OK:
                    {

                        File.Copy(_path, "Working_file.txt", true);
                        MessageBox.Show("Done Log File Selection");
                        // enter the progress bar
                        if (backgroundWorker1.IsBusy != true)
                        {
                            // create a new instance of the alert form
                            _alert = new AlertForm();
                            _alert.Show();
                            // Start the asynchronous operation.
                            backgroundWorker1.RunWorkerAsync();
                        }
                        // This event handler cancels the backgroundworker, fired from Cancel button in AlertForm.

                        _alert.Canceled += new EventHandler<EventArgs>(buttonCancel_Click);
                        // start timer for load time
                        timer1.Interval = 1000;
                        timer1.Start();
                        break;
                    }
                case DialogResult.Cancel:
                    {
                        MessageBox.Show("ALERT: File Selection exited!");


                        break;
                    }
            }
        }
        // this is the cancel button on the parse
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            if (backgroundWorker1.WorkerSupportsCancellation == true)
            {
                // Cancel the asynchronous operation.
                backgroundWorker1.CancelAsync();
                // Close the AlertForm
                _alert.Close();

            }
        }
        // timer for log load time
        private void timer1_Tick_1(object sender, EventArgs e)
        {
            _logLoadTimer = _logLoadTimer + 1;

        }

        // this is where we actually parse the file as a background task as to not freeze your pc
        private void backgroundWorker1_DoWork_1(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            ///connect to Database
            string connectionString = "Data Source = (LocalDB)\\MSSQLLocalDB; AttachDbFilename = |DataDirectory|\\Database1.mdf ; Integrated Security = True; Connect Timeout = 30";

            using (SqlConnection openCon = new SqlConnection(connectionString))
            {

                try
                {
                    openCon.Open();

                    //MessageBox.Show("Database opened");
                }
                catch
                {
                    openCon.Close();
                    MessageBox.Show("Error opening database");

                }
                try
                {
                    /// clear heal table of old parses

                    SqlCommand cmd2 = new SqlCommand();
                    cmd2 = new SqlCommand("TRUNCATE TABLE [TotalHeals]", openCon);
                    cmd2.ExecuteNonQuery();

                }
                catch
                {
                    MessageBox.Show("Error clearing old table data");
                }

                /// Stream Read the TXT File line by line

                // progress bar counters
                int progress = 0;
                int percent = 0;
                _healCount = 0;

                using (StreamReader reader = new StreamReader("Working_file.Txt"))
// Main Loop 1 for reading file
                    while (reader.EndOfStream == false)
                    { 
                        //if cancellation is pending, cancel work.  
                        if (backgroundWorker1.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }


                        // read fist line
                        string line = reader.ReadLine();




                        // stuff for the loading bar
                        progress = progress + 1;
                        if (progress >= 20000)
                        {
                            percent = percent + 1;
                            progress = 0;
                            if (percent == 100)
                            {
                                percent = 0;
                                MessageBox.Show("Your log file needs maintence because it is getting to large. You may continue to wait or press cancel and use the log chunker button.");
                            }

                        }

                        worker.ReportProgress(percent);
                        // end stuff


                        if (line == null)
                            break;

                        /// Fix the time stamp issue
                        Regex yourRegex = new Regex(@"\[([^\}]+)\]");
                        string fixedline = yourRegex.Replace(line, "");
                        fixedline.Replace("'", "''");


                        /// Fix chat

                        string[] items = fixedline.Split('.');
/// Line 1 loop 
                        foreach (string item in items)
                        {


                            if (item.IndexOf("tells") != -1)
                            {
                                fixedline = "";

                            }
                            if (item.IndexOf("say") != -1)
                            {
                                fixedline = "";
                            }
                            if (item.IndexOf("told") != -1)
                            {
                                fixedline = "";
                            }
// Self Contained Loop for line 1  For Encounter Detection
                            string fixedline3 = fixedline;
                            string[] items2 = fixedline3.Split('.');
                            
                            foreach (string item2 in items2)
                            {
                                // detect mob name


                               


                                // detect mob name
                                if (fixedline3 != "")
                                {

                                    if (item2.Contains("non-melee damage") & item2.Contains("for"))
                                    {
                                        string parts6 = fixedline3.Substring(0, item2.IndexOf("for"));
                                        // special case to detect spells landing
                                        if (parts6.Contains("hit"))
                                        {
                                            _mob = parts6.Substring(parts6.IndexOf("hit") + 4);
                                        }
                                    }

                                    if (item2.Contains("damage") & item2.Contains("for"))
                                    {
                                        string parts5 = fixedline3.Substring(0, item2.IndexOf("for"));
                                        if (parts5.Contains("crushes"))
                                        {
                                            _mob = parts5.Substring(parts5.IndexOf("crushes") + 8);

                                        }
                                        if (parts5.Contains("slashes"))
                                        {
                                            _mob = parts5.Substring(parts5.IndexOf("slashes") + 8);


                                        }
                                        if (parts5.Contains("hits"))
                                        {

                                            if (_encounterName.ToLower() != parts5.Substring(0, parts5.IndexOf("hits") - 1).ToLower())
                                            {
                                                _mob = parts5.Substring(parts5.IndexOf("hits") + 5);
                                            }
                                            else
                                            {
                                                _mob = parts5.Substring(0, parts5.IndexOf("hits") - 1);
                                            }

                                        }
                                        if (parts5.Contains("pierces"))
                                        {
                                            _mob = parts5.Substring(parts5.IndexOf("pierces") + 8);

                                        }
                                        if (parts5.Contains("kicks"))
                                        {
                                            _mob = parts5.Substring(parts5.IndexOf("kicks") + 6);

                                        }
                                        // only detects mobs biting
                                        if (parts5.Contains("bites"))
                                        {

                                            if (_encounterName.ToLower() != parts5.Substring(0, parts5.IndexOf("bites") - 1).ToLower())
                                            {
                                                _mob = parts5.Substring(parts5.IndexOf("bites") + 6);
                                            }
                                            else
                                            {
                                                _mob = parts5.Substring(0, parts5.IndexOf("bites") - 1);
                                            }



                                        }
                                        //only detects mob clawing
                                        if (parts5.Contains("claws"))
                                        {
                                            if (_encounterName.ToLower() != parts5.Substring(0, parts5.IndexOf("claws") - 1).ToLower())
                                            {
                                                _mob = parts5.Substring(parts5.IndexOf("claws") + 6);
                                            }
                                            else
                                            {
                                                _mob = parts5.Substring(0, parts5.IndexOf("claws") - 1);
                                            }

                                        }
                                        if (parts5.Contains("punches"))
                                        {
                                            _mob = parts5.Substring(parts5.IndexOf("punches") + 8);

                                        }

                                        // special case for pets hitting
                                        if (parts5.Contains("pet hits"))
                                        {
                                            _mob = parts5.Substring(parts5.IndexOf("hits") + 4);
                                        }
                                        // special case for lava type damage shields
                                        if (parts5.Contains("burned"))
                                        {
                                            _mob = parts5.Substring(0, parts5.IndexOf("burned") - 4);
                                        }
                                        // special case for thorn type damage shields
                                        if (parts5.Contains("pierced"))
                                        {
                                            _mob = parts5.Substring(0, parts5.IndexOf("pierced") - 4);
                                        }
                                        // special case for cold type damage shields
                                        if (parts5.Contains("tormented"))
                                        {
                                            _mob = parts5.Substring(0, parts5.IndexOf("tormented") - 4);
                                        }

                                        if (parts5.Contains("backstabs"))
                                        {
                                            _mob = parts5.Substring(parts5.IndexOf("backstabs") + 10);

                                        }

                                        // only detects mobs bashing not players
                                        if (parts5.Contains("bashes"))
                                        {
                                            if (_encounterName.ToLower() != parts5.Substring(0, parts5.IndexOf("bashes") - 1).ToLower())
                                            {
                                                _mob = parts5.Substring(parts5.IndexOf("bashes") + 7);
                                            }
                                            else
                                            {
                                                _mob = parts5.Substring(0, parts5.IndexOf("bashes") - 1);
                                            }

                                        }

                                    }
                                    do
                                    {
                                        _encounterName = _mob;
                                        _currentMob = _encounterName;
                                       
                                    }
                                    while (_encounterName == null);


                                    if (_currentMob != _encounterName)
                                    {

                                        _currentMobDead = false;
                                    }
                                    else
                                    {
                                                                                                           
                                        _currentMobDead = true;
                                        _currentMob = _encounterName;
                                       
                                    }

                                    _encounterName = _mob;

                                    if (fixedline3.Contains("has been slain") & !(fixedline3.Contains("pet")))
                                    
                                    {
                                        
                                        string parts6 = fixedline3.Substring(0, (fixedline3.IndexOf("slain")-1));
                                        if (parts6.Contains(_encounterName))
                                        {
                                          
                                            //MessageBox.Show(" mob has been slain triggered");
                                            //write encounter
                                            try
                                            {



                                                string command = "INSERT INTO EncounterTable(Encounter) VALUES(@Encounter)";
                                                SqlCommand cmd6 = new SqlCommand(command, openCon);
                                                cmd6.Parameters.AddWithValue("@Encounter", _encounterName);
                                                cmd6.ExecuteNonQuery();




                                            }
                                            catch (Exception ex)
                                            {

                                                MessageBox.Show(ex.ToString() + "Error Writing Encouter Press ok to continue");
                                            }
                                            


                                        }


                                    }
                                    



                                }
                                            
                                        
                                       
                                   

                            
                            }
                            // end Self Contained Loop for line 2 For Encounter Detection



                            /*   // this works on others spells not yours
                               ///get spell name Variable used is SpellNameFinal
                               if (item.IndexOf("begins to cast") != -1)
                               {
                                   string[] parts = Regex.Split(fixedline, ">");
                                   var list3 = new List<string>();
                                   for (int i = 0; i < parts.Length; i++)
                                   {
                                       if (parts[i].Contains("<"))
                                       {
                                           string[] parts2 = Regex.Split(parts[i], "<");

                                           SpellName = parts2[1];
                                           SpellNamefinal = SpellName.Replace("'", "''");
                                           // Create a list of spells cast for troubleshooting
                                           list3.Add(SpellNamefinal);


                                       }
                                   }

                                   */



                            // Find out what spell your casting in line 1
                            if (item.IndexOf("You begin casting") != -1)
                            {

                                // add code to find spell name cast
                                string[] parts2 = Regex.Split(fixedline, "casting");
                                _spellNamefinal = parts2[1];
                                if (_spellNamefinal.Contains("'"))
                                {
                                    _spellNamefinal = _spellNamefinal.Replace("'", " ");
                                }
                                if (_spellNamefinal.Contains("."))
                                {
                                    _spellNamefinal = _spellNamefinal.Replace(".", "");
                                }

                                _healCount++;


                                //what to search for while your casting loop
                                while ((line.Contains("You begin casting")))
                                {

                                    // Read Line 2
                                    string line2 = reader.ReadLine();


                                    if (line2 == null)
                                    {
                                        break;
                                    }
                                    /// Fix the time stamp issue line 2
                                    Regex yourRegex2 = new Regex(@"\[([^\}]+)\]");
                                    fixedline2 = yourRegex2.Replace(line2, "");
                                    fixedline2.Replace("'", "''");


                                    // code to detect Tunare's Grace special aa Heal
                                    string[] items3 = Regex.Split(fixedline2, "points");
                                    // Self contained loop for line 2 For Tunares Grace Proc going off
                                    foreach (string item3 in items3)
                                    {
                                        if (item3.IndexOf("Tunare's Grace") != -1)
                                        {
                                            string[] parts3 = Regex.Split(fixedline2, "points");
                                            for (int i = 0; i < parts3.Length; i++)
                                            {
                                                if (parts3[i].Contains("for"))
                                                {
                                                    string[] parts4 = Regex.Split(parts3[i], "for");

                                                    _playerHeal = parts4[1];
                                                }
                                            }

                                            _playerHealAmt = int.Parse(_playerHeal);
                                            _totalHealAmt = _totalHealAmt + _playerHealAmt;
                                            _spellPlayerName = "You";
                                            _healee = "You";
                                            _spellNamefinal = "Tunares Grace";
                                           


                                                try
                                                {

                                                    SqlCommand cmd4 = new SqlCommand();
                                                    //write heals

                                                    cmd4 = new SqlCommand("INSERT INTO TotalHeals(TotalHeal,TotalHealCT,PlayerName,SpellName,Encounter,EachHeal,Healee) VALUES ('" + _totalHealAmt + "','" + _healCount + "','" + _spellPlayerName + "','" + _spellNamefinal + "','" + _encounterName + "','" + _playerHealAmt + "','" + _healee + "')", openCon);
                                                    cmd4.ExecuteNonQuery();




                                                }
                                                catch (Exception ex)
                                                {

                                                    MessageBox.Show(ex.ToString() + "Press ok to continue");
                                                }
                                            
                                        }

                                    }
                                    // end Self contained loop for line 2 For Tunares Grace Proc going off
                                    /// Fix chat line 2


                                    string[] items4 = fixedline2.Split('.');
                                    // Self Contained Loop for Line2  for who your healing
                                    foreach (string item4 in items4)
                                    {


                                        if (item4.IndexOf("tells") != -1)
                                        {
                                            fixedline2 = "";

                                        }
                                        if (item4.IndexOf("say") != -1)
                                        {
                                            fixedline2 = "";
                                        }
                                        if (item4.IndexOf("told") != -1)
                                        {
                                            fixedline2 = "";
                                        }

                                        // trigger You have healed Exor for 8070 points.
                                        if (item4.IndexOf("You have healed") != -1)
                                        {
                                            _playerHeal = Regex.Match(item4, @"\d+").Value;
                                            _playerHealAmt = int.Parse(_playerHeal);
                                            _totalHealAmt = _totalHealAmt + _playerHealAmt;
                                            _spellPlayerName = "You";
                                            // add hunt for player healed here
                                            // code for locating healee's name                                    
                                            string[] parts3 = Regex.Split(fixedline2, "for");
                                            for (int i = 0; i < parts3.Length; i++)
                                            {
                                                if (parts3[i].Contains("healed"))
                                                {
                                                    string[] parts4 = Regex.Split(parts3[i], "healed");

                                                    _healee = parts4[1];
                                                }
                                            }

                                            // triggger You have healed Krippalzz for 8500 hit points with your Abundant Healing XLIV.
                                            // code to detect Abundant Healing special aa Heal
                                            if (item4.Contains("Abundant Healing"))
                                            {
                                                //MessageBox.Show(" abundant healing working");
                                                _spellNamefinal = "Abundant Healing";

                                            }
                                            //fix for a Hireling as a healee
                                            if (_healee.Contains("hireling"))
                                            {
                                                _healee = "Merc";
                                            }
                                            // fix for aa pets being healee
                                            if (_healee.Contains("pet"))
                                            {
                                                _healee = "AAPet";
                                            }
                                            // fix for healee with more then 1 name
                                            _healee = _healee.Replace(" ", "");
                                            // bug fix 5-31-16 fix for warder's and familurs as healee
                                            _healee = _healee.Replace("`", "");



                                           



                                            try
                                            {

                                                SqlCommand cmd = new SqlCommand();
                                                if (_healee == null)
                                                {
                                                    _healee = "No Target";
                                                }



                                                //write heals
                                                cmd = new SqlCommand("INSERT INTO TotalHeals(TotalHeal,TotalHealCT,PlayerName,SpellName,Encounter,EachHeal,Healee) VALUES ('" + _totalHealAmt + "','" + _healCount + "','" + _spellPlayerName + "','" + _spellNamefinal + "','" + _encounterName + "','" + _playerHealAmt + "','" + _healee + "')", openCon);
                                                cmd.ExecuteNonQuery();
                                                



                                            }
                                            catch (Exception ex)
                                            {

                                                MessageBox.Show( ex.ToString() + "Press ok to continue");
                                            }




                                        }
                                    }
                                    // End Self Contained Loop for Line2  for who your healing



                                    if (_currentMobDead == true)
                                   // if (line2.Contains("You begin casting"))
                                    {
                                        break;
                                    }
                                   

                                    

                                }
                                    // End what to search for while your casting loop
                            }
                        }
// End Line 1 Loop
                
                    }
// End Streamer Close the file
                openCon.Close();
            }
// End using statement for the open connection to database

        }
// end background worker
       
    

           
       
        // show the alert form the progress the background task has done

        private void backgroundWorker1_ProgressChanged_1(object sender, ProgressChangedEventArgs e)
        {
            // Show the progress in main form (GUI)
            labelResult.Text = (e.ProgressPercentage.ToString() + "%");
            // Pass the progress to AlertForm label and progressbar
            _alert.Message = "In progress, please wait... " + e.ProgressPercentage.ToString() + "%";
            _alert.ProgressValue = e.ProgressPercentage;
        }
        // when the background task finsished do this....
        private void backgroundWorker1_RunWorkerCompleted_1(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                labelResult.Text = "Canceled!";
            }
            else if (e.Error != null)
            {
                labelResult.Text = "Error: " + e.Error.Message;
            }
            else
            {

                labelResult.Text = "Log Loaded in: " + _logLoadTimer + " Seconds";
            }
            // Close the AlertForm
            timer1.Stop();
            _alert.Close();
            MessageBox.Show(" Done Parse");
            // this displays the main data table

            SqlDataAdapter totalHealsTableAdapter1 = new SqlDataAdapter();
            DataSet database1DataSet1 = new DataSet();


            _sql = "select * from TotalHeals";
            _connetionString = "Data Source = (LocalDB)\\MSSQLLocalDB; AttachDbFilename = |DataDirectory|\\Database1.mdf; Integrated Security = True; Connect Timeout = 30";
            _connection = new SqlConnection(_connetionString);
            try
            {
                _connection.Open();
                totalHealsTableAdapter1 = new SqlDataAdapter(_sql, _connection);
                totalHealsTableAdapter1.Fill(database1DataSet1);
                _connection.Close();
                dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dataGridView1.MultiSelect = false;
                dataGridView1.DataSource = database1DataSet1.Tables[0];
               

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            // this displays the encounter table
            SqlDataAdapter totalHealsTableAdapter = new SqlDataAdapter();
            DataSet database1DataSet = new DataSet();


            _sql = "select Encounter from TotalHeals";
            _connetionString = "Data Source = (LocalDB)\\MSSQLLocalDB; AttachDbFilename = |DataDirectory|\\Database1.mdf; Integrated Security = True; Connect Timeout = 30";
            _connection = new SqlConnection(_connetionString);
            try
            {
                _connection.Open();
                totalHealsTableAdapter = new SqlDataAdapter(_sql, _connection);
                totalHealsTableAdapter.Fill(database1DataSet);
                _connection.Close();
                dataGridView2.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dataGridView2.MultiSelect = false;
                dataGridView2.DataSource = database1DataSet1.Tables[0];
                

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            // this displays the encounter table
            SqlDataAdapter encounterTableTableAdapter  = new SqlDataAdapter();
            DataSet database1DataSet2 = new DataSet();


            _sql = "select * from EncounterTable";
            _connetionString = "Data Source = (LocalDB)\\MSSQLLocalDB; AttachDbFilename = |DataDirectory|\\Database1.mdf; Integrated Security = True; Connect Timeout = 30";
            _connection = new SqlConnection(_connetionString);
            try
            {
                _connection.Open();
                encounterTableTableAdapter = new SqlDataAdapter(_sql, _connection);
                encounterTableTableAdapter.Fill(database1DataSet2);
                _connection.Close();
                dataGridView3.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dataGridView3.MultiSelect = false;
                dataGridView3.DataSource = database1DataSet2.Tables[0];


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }


        }
        private void dataGridView1_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex > -1 && e.RowIndex > -1)
            {

                if (dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value != null)
                {

                    _userView1 = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                    MessageBox.Show(dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() + " Selected");
                    // this displays the filtered data

                    SqlDataAdapter totalHealsTableAdapter1 = new SqlDataAdapter();
                    DataSet database1DataSet1 = new DataSet();


                    _sql = "select * from TotalHeals where Encounter = '" + _userView1 + "'";
                    _connetionString = "Data Source = (LocalDB)\\MSSQLLocalDB; AttachDbFilename = |DataDirectory|\\Database1.mdf; Integrated Security = True; Connect Timeout = 30";
                    _connection = new SqlConnection(_connetionString);
                    try
                    {
                        _connection.Open();
                        totalHealsTableAdapter1 = new SqlDataAdapter(_sql, _connection);
                        totalHealsTableAdapter1.Fill(database1DataSet1);
                        _connection.Close();
                        dataGridView2.SelectionMode = DataGridViewSelectionMode.CellSelect;
                        dataGridView2.MultiSelect = false;
                        dataGridView2.DataSource = database1DataSet1.Tables[0];
                       
                        labelResult.Text = "Sorting by" + _userView1;

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }

                }
            }
        }



        private void dataGridView2_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex > -1 && e.RowIndex > -1)
            {
                if (dataGridView2.Rows[e.RowIndex].Cells[e.ColumnIndex].Value != null & _userView1 != null)
                {

                    _userView2 = dataGridView2.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                    MessageBox.Show(dataGridView2.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() + " Selected");
                    // this displays the filtered data

                    SqlDataAdapter totalHealsTableAdapter1 = new SqlDataAdapter();
                    DataSet database1DataSet1 = new DataSet();


                    _sql = "select * from TotalHeals where Encounter = '" + _userView1 + "'AND Healee = '" + _userView2 + "'";
                    _connetionString = "Data Source = (LocalDB)\\MSSQLLocalDB; AttachDbFilename = |DataDirectory|\\Database1.mdf; Integrated Security = True; Connect Timeout = 30";

                    _connection = new SqlConnection(_connetionString);
                    try
                    {
                        _connection.Open();
                        totalHealsTableAdapter1 = new SqlDataAdapter(_sql, _connection);
                        totalHealsTableAdapter1.Fill(database1DataSet1);
                        _connection.Close();
                        dataGridView2.SelectionMode = DataGridViewSelectionMode.CellSelect;
                        dataGridView2.MultiSelect = false;
                        dataGridView2.DataSource = database1DataSet1.Tables[0];


                        labelResult.Text = "Sorting by" + _userView1 + " and " + _userView2;
                        //add stats window here

                        _stats = new Stats();
                        _stats.Healee = _userView2;
                        _stats.Encounter = _userView1;
                        SqlDataAdapter totalHealsTableAdapter3 = new SqlDataAdapter();
                        DataSet database1DataSet3 = new DataSet();


                        _sql2 = "select * from TotalHeals where Encounter = '" + _userView1 + "'AND Healee = '" + _userView2 + "'";
                        _connetionString = "Data Source = (LocalDB)\\MSSQLLocalDB; AttachDbFilename = |DataDirectory|\\Database1.mdf; Integrated Security = True; Connect Timeout = 30";

                        _connection = new SqlConnection(_connetionString);

                        _connection.Open();
                        totalHealsTableAdapter3 = new SqlDataAdapter(_sql2, _connection);
                        totalHealsTableAdapter3.Fill(database1DataSet3);
                        //calc stats for total heals
                        SqlCommand _sql3 = new SqlCommand("SELECT COUNT(*) FROM TotalHeals where Encounter = '" + _userView1 + "'AND Healee = '" + _userView2 + "'", _connection);
                        try
                        {
                            Int32 count = Convert.ToInt32(_sql3.ExecuteScalar());
                            if (count > 0)
                            {
                                _stats.TotalCastsLabel.Text = Convert.ToString(count.ToString());
                            }
                            else
                            {
                                _stats.TotalCastsLabel.Text = "0";
                            }
                        }
                        catch 
                        {
                            MessageBox.Show("Try Picking an Encounter first, then a Healee.");
                        }
                          

                        
                        // calc stats for total healed
                        SqlCommand _sql4 = new SqlCommand("SELECT SUM (EachHeal) FROM TotalHeals where Encounter = '" + _userView1 + "'AND Healee = '" + _userView2 + "'", _connection);
                        try
                        {
                            Int32 totalheals = Convert.ToInt32(_sql4.ExecuteScalar());
                            if (totalheals > 0)
                            {
                                _stats.TotalHealsLabel.Text = Convert.ToString(totalheals.ToString());
                            }
                            else
                            {
                                _stats.TotalHealsLabel.Text = "0";
                            }
                        }
                        catch 
                        {
                            MessageBox.Show("Try Picking an Encounter first, then a Healee.");
                        }

                        // close the connection to the databaase
                        _connection.Close();
                         _stats.dataGridView1.DataSource = database1DataSet3.Tables[0];
                        _stats.totalHealsTableAdapter3.Fill(_stats.database1DataSet3.TotalHeals);
                        

                  
                        
                         _stats.Show();
                         
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }













                }
            }

        }

        // this is the log chunker sub program

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Log Chunking will create numerous 25 MB new logs of your currently selected log file. It will leave the original log intact. ");
            DialogResult eqlogfile2 = openFileDialog2.ShowDialog();
            _path = openFileDialog2.FileName;

            switch (eqlogfile2)
            {
                case DialogResult.OK:
                    {

                        string sourceFileName = _path;

                        string destFileLocation = Path.GetDirectoryName(_path); ;

                        int index = 0;

                        long maxFileSize = 26214400;

                        byte[] buffer = new byte[65536];



                        using (System.IO.Stream source = File.OpenRead(sourceFileName))

                        {

                            while (source.Position < source.Length)

                            {

                                index++;



                                // Create a new sub File, and read into it

                                string newFileName = Path.Combine(destFileLocation, Path.GetFileNameWithoutExtension(sourceFileName));

                                newFileName += index.ToString() + Path.GetExtension(sourceFileName);

                                using (System.IO.Stream destination = File.OpenWrite(newFileName))

                                {

                                    while (destination.Position < maxFileSize)

                                    {

                                        // Work out how many bytes to read

                                        int bytes = source.Read(buffer, 0, (int)Math.Min(maxFileSize, buffer.Length));

                                        destination.Write(buffer, 0, bytes);



                                        // Are we at the end of the file?

                                        if (bytes < Math.Min(maxFileSize, buffer.Length))

                                        {

                                            break;

                                        }

                                    }

                                }

                            }
                        }
                        MessageBox.Show("Done Log Chunking");

                        break;
                    }

                case DialogResult.Cancel:
                    {
                        MessageBox.Show("ALERT: Log Chunker Exited!");


                        break;
                    }
            }

        
    }

        private void Form1_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the 'database1DataSet2.EncounterTable' table. You can move, or remove it, as needed.
            this.encounterTableTableAdapter.Fill(this.database1DataSet2.EncounterTable);



        }
        // clears drilled down selections and displays defualts

        private void button3_Click(object sender, EventArgs e)
        {
            // this displays the main data table

            SqlDataAdapter totalHealsTableAdapter1 = new SqlDataAdapter();
            DataSet database1DataSet1 = new DataSet();
            // reset the encounter data
            _userView1 = null;

            _sql = "select * from TotalHeals";
            _connetionString = "Data Source = (LocalDB)\\MSSQLLocalDB; AttachDbFilename = |DataDirectory|\\Database1.mdf; Integrated Security = True; Connect Timeout = 30";
            _connection = new SqlConnection(_connetionString);
            try
            {
                _connection.Open();
                totalHealsTableAdapter1 = new SqlDataAdapter(_sql, _connection);
                totalHealsTableAdapter1.Fill(database1DataSet1);
                _connection.Close();
                dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dataGridView1.MultiSelect = false;
                dataGridView1.DataSource = database1DataSet1.Tables[0];


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            // this displays the encounter table
            SqlDataAdapter totalHealsTableAdapter = new SqlDataAdapter();
            DataSet database1DataSet = new DataSet();


            _sql = "select Encounter from TotalHeals";
            _connetionString = "Data Source = (LocalDB)\\MSSQLLocalDB; AttachDbFilename = |DataDirectory|\\Database1.mdf; Integrated Security = True; Connect Timeout = 30";
            _connection = new SqlConnection(_connetionString);
            try
            {
                _connection.Open();
                totalHealsTableAdapter = new SqlDataAdapter(_sql, _connection);
                totalHealsTableAdapter.Fill(database1DataSet);
                _connection.Close();
                dataGridView2.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dataGridView2.MultiSelect = false;
                dataGridView2.DataSource = database1DataSet1.Tables[0];
                labelResult.Text = "Sorting Cleared";


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        
    }

}
