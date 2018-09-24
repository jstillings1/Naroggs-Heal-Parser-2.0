using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HealParser
{

    public partial class Stats : Form
    {
        #region PROPERTIES
        public string Healee
        {
            set { HealeeLabel.Text = value; }
        }

        public string Encounter
        {
            set { EncounterLabel.Text = value; }
        }
        public int TotalHeals
        {
            set { TotalHealsLabel.Text = value.ToString(); }
        }
        public int TotalCasts
        {
            set { TotalCastsLabel.Text = value.ToString(); }
        }
        
        
        

        #endregion
        public Stats()
        {
            InitializeComponent();
            
      

        }

        private void Stats_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the 'database1DataSet3.TotalHeals' table. You can move, or remove it, as needed.
           
            // TODO: This line of code loads data into the 'database1DataSet2.TotalHeals' table. You can move, or remove it, as needed.
          
            // TODO: This line of code loads data into the 'database1DataSet1.TotalHeals' table. You can move, or remove it, as needed.
          
            // TODO: This line of code loads data into the 'database1DataSet4.TotalHeals' table. You can move, or remove it, as needed.

            // TODO: This line of code loads data into the 'database1DataSet3.Table' table. You can move, or remove it, as needed.








        }
    }
}
