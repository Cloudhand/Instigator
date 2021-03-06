﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioSampler
{
    public partial class FormSampler : Form
    {
       //constants
        private const int COLLAPSED_HEIGHT = 510;     //required to shift expand/collapse bar
        private const int EXPANDED_HEIGHT = 784;      //

        delegate void SetPictureBoxImageCallBack(PictureBox pBox, Image img); //need these for multithreading
        delegate void OutputTextCallBack(string text);
        
        private bool isPanelExpanded;
        private Image imgPadBlack = AudioSampler.Properties.Resources.Black_Pad;
        private Image imgPadYellow = AudioSampler.Properties.Resources.Yellow_Pad;
        private Image imgPadBlackSelected = AudioSampler.Properties.Resources.Black_Pad_Selected;
        private Image imgPadYellowSelected = AudioSampler.Properties.Resources.Yellow_Pad_Selected;
        private Image imgUpArrow = AudioSampler.Properties.Resources.TogglePanelButtonUp;
        private Image imgDownArrow = AudioSampler.Properties.Resources.TogglePanelButtonDown;
        private Pad pad1;
        private Pad pad2;
        private Pad pad3;
        private Pad pad4;
        private Pad pad5;
        private Pad pad6;
        private Pad pad7;
        private Pad pad8;
        private Pad pad9;
        private Player player1;
        private Player player2;
        private Player player3;
        private Player player4;
        private Player player5;
        private Player player6;
        private Player player7;
        private Player player8;
        private Player player9;
        private Pad selectedPad;  //this is used for determining which pad is being edited

        private List<Player> playerList; //we'll need a player for each pad
        private List<Pad> padList;  //some functions will be easier with a foreach of all the pads
        
        private bool isDebugging = true;  //setting to true displays the debug textbox

        //////////////////////////////////////////////////////////////////
        //  Constructor
        // 
        // Draws form, initializes values
        //////////////////////////////////////////////////////////////////
        public FormSampler()
        {
           InitializeComponent();
           if (isDebugging)
           {
              txtBoxDebug.Visible = true;
              lblDebug.Visible = true;
           }

           InitializePads();
           InitializePlayers();
           CollapsePanel();
           LoadEffectsPanels();
        }

        //////////////////////////////////////////////////////////////////
        //  Form interaction methods
        // 
        // mouse events, etc
        //////////////////////////////////////////////////////////////////
        private void pictureBox1_Click(object sender, EventArgs e)
        { 
           if (isPanelExpanded)
              CollapsePanel();
           else
              ExpandPanel();
        }

        private void loadToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //to be implemented later:
            //load "kit" file
            //includes samples, effects settings for all pads
        }
        private void saveToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //to be implemented later:
            //save "kit" file
            //includes samples, effects settings for all pads
        }
        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //to be implemented later:
            //prompt to save changes
            //stop playback, dispose of players and associated objects
            //dispose of form
        }
        private void fx1OnCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (fx1OnCheckBox.Checked) 
                //need to make sure the check came from a user event, 
                //not from changing the selected pad
            {
                //TODO:disable other effects... for current single-effect functionality

                //set the selected pads effect
                selectedPad.parametersChanged = true;
                selectedPad.currentFilter = 1;
                selectedPad.isEchoing = true;
                OutputTextLine("Echo enabled");
            }
            else
            {
                selectedPad.isEchoing = false;
                OutputTextLine("Echo disabled");
                //TODO: Check if other text boxes are enabled, otherwise reset currentFilter to 0;
                selectedPad.currentFilter = 0;
            }
            OutputTextLine("currentFilter = " + selectedPad.currentFilter.ToString());
        }
        private void btnEcho_Click(object sender, EventArgs e)
        {
            //TODO: Need to validate data either here or in pad, pad-side validation preferred
            try
            {
                selectedPad.parametersChanged = true;
                selectedPad.echoCount = Convert.ToInt32(txtBoxEchoCount.Text);
                selectedPad.echoDelay = Convert.ToInt32(txtBoxEchoDelay.Text);
                selectedPad.echoFactor = (float)Convert.ToDouble(txtBoxEchoFactor.Text);
                btnEcho.Enabled = false;
            }
            catch (Exception ex)
            {
                OutputTextLine("Couldn't apply settings: " + ex.Message);
            }
        }

        private void btnChorus_Click(object sender, EventArgs e)
        {
            //TODO: Need to validate data either here or in pad, pad-side validation preferred
            try
            {
                selectedPad.parametersChanged = true;
                selectedPad.chorusDelay = Convert.ToInt32(txtBoxChorusDelay.Text);
                selectedPad.chorusDepth = Convert.ToInt32(txtBoxChorusDepth.Text);
                selectedPad.chorusLevel = Convert.ToDouble(txtBoxChorusLevel.Text);
                btnChorus.Enabled = false;
            }
            catch (Exception ex)
            {
                OutputTextLine("Couldn't apply settings: " + ex.Message);
            }
        }
        private void btnFilter_Click(object sender, EventArgs e)
        {
            try
            {
                selectedPad.parametersChanged = true;
                selectedPad.frequency = Convert.ToInt32(txtBoxFilterFrequency.Text);
                selectedPad.isLowPass = rbtnFilterLP.Checked;
                btnFilter.Enabled = false;
            }
            catch (Exception ex)
            {
                OutputTextLine("Couldn't apply settings: " + ex.Message);
            }
        }
        private void btnDrive_Click(object sender, EventArgs e)
        {
            try
            {
                selectedPad.parametersChanged = true;
                selectedPad.driveSetting = Convert.ToInt32(txtBoxFilterFrequency.Text);
                btnDrive.Enabled = false;
            }
            catch (Exception ex)
            {
                OutputTextLine("Couldn't apply settings: " + ex.Message);
            }
        }
        //////////////////////////////////////////////////////////////////
        //  Visual methods
        // 
        // toggle fx panel, light up pads
        //////////////////////////////////////////////////////////////////

        private void CollapsePanel()
        {
            this.Height = COLLAPSED_HEIGHT;
            pictureBoxExpand.Image = imgDownArrow;
            isPanelExpanded = false;
        }
       
       private void ExpandPanel()
        {
            this.Height = EXPANDED_HEIGHT;
            pictureBoxExpand.Image = imgUpArrow;
            isPanelExpanded = true;
        }

        private void SetPictureBoxImage(PictureBox pBox, Image img)
        {
           // InvokeRequired required compares the thread ID of
           // the calling thread to the thread ID of the 
           // creating thread.  If these threads are different, 
           // it returns true
           if (pBox.InvokeRequired)
           {  
              //basically, if the PictureBox (pBox) that is trying to be altered 
              //was created in a different thread than the one calling this method,
              //we make a clone of this method...  (using the previously declared delegate object)
              SetPictureBoxImageCallBack d = new SetPictureBoxImageCallBack(SetPictureBoxImage);
              //and have the original thread call our cloned method so it can execute safely
              this.Invoke(d, new object[] { pBox, img });
           }
           else
           {
              pBox.Image = img;
           }
        }

        //////////////////////////////////////////////////////////////////
        //  Pad methods
        // 
        // file loading, click, keypress, etc.
        //////////////////////////////////////////////////////////////////
        
        private void loadSampleToolStripMenuItem_Click(object sender, EventArgs e)  //ONLY TO BE CALLED BY CLICKING ON A PAD
        {   //Because we're using the same context menu for all pads,                 //To load sample other ways, use GetSampleFromDialog method
           //we'll have to figure out the object that was clicked on
           
            ToolStripItem menuItem = sender as ToolStripItem;
            if (menuItem != null)
            {
               // Retrieve the ContextMenuStrip that owns this ToolStripItem
               ContextMenuStrip owner = menuItem.Owner as ContextMenuStrip;
               if (owner != null)
               {
                  // Get the control that is displaying this context menu
                  Control sourceControl = owner.SourceControl;
                  Pad pad = (Pad)sourceControl;
                  GetSampleFromDialog(pad);
               }
            }
        }

        // Selects a file to be opened, saves that value to the pad
        private void GetSampleFromDialog(Pad pad)
        {
           try
           {
              OpenFileDialog fileDialog = new OpenFileDialog();

              fileDialog.Filter = "Wave File (*.wav|*.wav;"; //update later with MP3 support
              fileDialog.Title = "Select Sample for Pad #" +pad.Name.Substring(2,1);

              DialogResult dr = fileDialog.ShowDialog();
              if (dr != DialogResult.OK)
              {
                 OutputTextLine("DialogResult did not equal OK");
                 return;
              }
              pad.SamplePath = fileDialog.FileName;

              pad.Dialog = fileDialog;
              pad.parametersChanged = true;
              OutputTextLine("Sample loaded " + pad.SamplePath);
           }
           catch (Exception ex)
           {
              OutputTextLine("Error: " + ex.Message);
              OutputTextLine("Probably couldn't cast to Pad");
           }
        }

        //this method is called only when the pad is left-clicked,
        //This "selects" the pad for editing
        private void pad_Clicked(object sender, EventArgs e)
        {
            Pad tempPad = (Pad)sender;

            if (tempPad != selectedPad) //check to make sure redraw is necessary
            {
                //remove outline from previous pad
                SetPictureBoxImage(selectedPad, imgPadBlack);
                //reassign selection
                selectedPad = tempPad;
                SetPictureBoxImage(selectedPad, imgPadBlackSelected);
                LoadEffectsPanels();
            }

            //now we send the trigger along
            pad_Activated(sender, e);
        }
        //////////////////////////////////////////////////////////////////
        // Loads the effects panels with appropriate data from global selectedPad
        //////////////////////////////////////////////////////////////////
        private void LoadEffectsPanels()
        {
            //re-fill effects parameters
            //echo
            fx1OnCheckBox.Checked = selectedPad.isEchoing;
            txtBoxEchoCount.Text = selectedPad.echoCount.ToString();
            txtBoxEchoDelay.Text = selectedPad.echoDelay.ToString();
            txtBoxEchoFactor.Text = selectedPad.echoFactor.ToString();
            btnEcho.Enabled = false;

            //chorus
            fx2OnCheckBox.Checked = selectedPad.isChorusing;
            txtBoxChorusDelay.Text = selectedPad.chorusDelay.ToString();
            txtBoxChorusDepth.Text = selectedPad.chorusDepth.ToString();
            txtBoxChorusLevel.Text = selectedPad.chorusLevel.ToString();
            btnChorus.Enabled = false;

            //filter
            fx3OnCheckBox.Checked = selectedPad.isPassing;
            txtBoxFilterFrequency.Text = selectedPad.frequency.ToString();
            rbtnFilterLP.Checked = selectedPad.isLowPass;
            rbtnFilterHP.Checked = !selectedPad.isLowPass;
            btnFilter.Enabled = false;

            //drive
            fx4OnCheckBox.Checked = selectedPad.isOverdriving;
            txtBoxDriveAmount.Text = selectedPad.driveSetting.ToString(); //this may not be correct
            btnDrive.Enabled = false;
        }

        //this method is called any time the pad is triggered
        private void pad_Activated(object sender, EventArgs e)
        {
            //might want a try catch on this cast...
            Pad activatedPad = (Pad)sender;
            OutputTextLine(activatedPad.Name+" activated");

            //make a new thread for the playback
            Thread padThread = new Thread(() => PlayPad(activatedPad));
            padThread.Start();            
        }

        private void PlayPad(Pad padButton)   //ISSUE: After loading a sample for the first time, the first click doesn't play back.
        {
            int escapeCounter = 0;
            if (padButton == selectedPad)  //If it has outline, we need to keep outline
            {
                SetPictureBoxImage(padButton, imgPadYellowSelected); 
            }
            else 
            { 
                SetPictureBoxImage(padButton, imgPadYellow); 
            }
            
            //using seperate players for each button, we need to fetch the right player
            GetPlayer(padButton).playSample(padButton);

            /*  Moved filter playback logic to player class
            //switch to control which playing flow is used(different for filter/no filter
            switch (padButton.currentFilter)
            {
                case 0:
                    player.playSample(padButton);
                    break;
                case 1:  // pad.currentFilter = 1 for echo
                    player.playSampEcho(padButton);
                    break;
                default:
                    break;
            }*/

            if (!GetPlayer(padButton).LogMessage.Equals(""))
            {
                OutputTextLine(GetPlayer(padButton).LogMessage);
            }
            //wait until the player is done before changing color back
            do 
            {
                Thread.Sleep(20);
            } while (GetPlayer(padButton).IsPlaying && ++escapeCounter < 1000);
            //OutputTextLine("IsPlaying = "+GetPlayer(padButton).IsPlaying.ToString());
            //OutputTextLine("Escape counter value: "+escapeCounter);
            if (padButton == selectedPad)  //If it has outline, we need to keep outline
            {
                SetPictureBoxImage(padButton, imgPadBlackSelected);
            }
            else
            {
                SetPictureBoxImage(padButton, imgPadBlack);
            }
            OutputTextLine("Done playing " + padButton.Name);
            //the player built the proper waveforms and outputs, so this can be reset
            padButton.parametersChanged = false;
        }

        //////////////////////////////////////////////////////////////////
        // Returns the player that corresponds to the pad sent as a parameter
        // Needs a try catch in implementation for unhandled conversion exception
        //////////////////////////////////////////////////////////////////
        private Player GetPlayer(Pad pad)
        {
            string name = pad.Name;
            //returns the last character within the name string (ex. pad1 = 1)
            string numString = name.Substring(name.Length - 1, 1);
            //convert the number to a string
            int numInt = Convert.ToInt32(numString);
            //then use the number to reference index of the playerList
            return playerList[numInt - 1];
        }

        //////////////////////////////////////////////////////////////////
        //  Text output methods
        // (currently used for txtBoxDebug output)
        // 
        //////////////////////////////////////////////////////////////////

        //Translates the newline char for use in textboxes
        private string LocalizeText(string text)
        {
            text = text.Replace("\n", Environment.NewLine);
            return text;
        }
        private void OutputText(string newText)
        {
           if (this.txtBoxDebug.InvokeRequired)
           {
              OutputTextCallBack ot = new OutputTextCallBack(OutputText);
              this.Invoke(ot, new object[] { newText });
           }
           else
           {
              txtBoxDebug.AppendText(LocalizeText(newText));
           }
        }
        private void OutputTextLine(string newLine)
        {
            OutputText(newLine + Environment.NewLine);
        }
        private void ClearOutputText()
        {
            txtBoxDebug.Clear();
        }

        /// <summary>
        /// Initializes and draws the pads on the form
        /// 
        /// in the future this will use a foreach for less code.
        /// but it works for now
       /// </summary>
        private void InitializePads()           
        {                                        
            this.pad1 = new AudioSampler.Pad();
            this.pad2 = new AudioSampler.Pad();
            this.pad3 = new AudioSampler.Pad();
            this.pad4 = new AudioSampler.Pad();
            this.pad5 = new AudioSampler.Pad();
            this.pad6 = new AudioSampler.Pad();
            this.pad7 = new AudioSampler.Pad();
            this.pad8 = new AudioSampler.Pad();
            this.pad9 = new AudioSampler.Pad();

            //load the padList
            padList = new List<Pad>()
            {
               this.pad1,
               this.pad2,
               this.pad3,
               this.pad4,
               this.pad5,
               this.pad6,
               this.pad7,
               this.pad8,
               this.pad9
            };
         
            // panelPads
            this.panelPads.BackColor = System.Drawing.SystemColors.InactiveCaption;
            this.panelPads.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelPads.Controls.Add(this.pad3);
            this.panelPads.Controls.Add(this.pad6);
            this.panelPads.Controls.Add(this.pad9);
            this.panelPads.Controls.Add(this.pad2);
            this.panelPads.Controls.Add(this.pad5);
            this.panelPads.Controls.Add(this.pad8);
            this.panelPads.Controls.Add(this.pad1);
            this.panelPads.Controls.Add(this.pad4);
            this.panelPads.Controls.Add(this.pad7);

            // pad3
            this.pad3.ContextMenuStrip = this.padContextMenuStrip;
            this.pad3.Location = new System.Drawing.Point(259, 256);
            this.pad3.Name = "pad3";
            this.pad3.Click += new System.EventHandler(this.pad_Clicked);
         
              // pad6
            this.pad6.ContextMenuStrip = this.padContextMenuStrip;
            this.pad6.Location = new System.Drawing.Point(256, 131);
            this.pad6.Name = "pad6";
            this.pad6.Click += new System.EventHandler(this.pad_Clicked);
         
            // pad9
            this.pad9.ContextMenuStrip = this.padContextMenuStrip;
            this.pad9.Location = new System.Drawing.Point(259, 6);
            this.pad9.Name = "pad9";
            this.pad9.Click += new System.EventHandler(this.pad_Clicked);
         
            // pad2
            this.pad2.ContextMenuStrip = this.padContextMenuStrip;
            this.pad2.Location = new System.Drawing.Point(134, 256);
            this.pad2.Name = "pad2";
            this.pad2.Click += new System.EventHandler(this.pad_Clicked);
          
            // pad5
            this.pad5.ContextMenuStrip = this.padContextMenuStrip;
            this.pad5.Location = new System.Drawing.Point(134, 131);
            this.pad5.Name = "pad5";
            this.pad5.Click += new System.EventHandler(this.pad_Clicked);
          
            // pad8
            this.pad8.ContextMenuStrip = this.padContextMenuStrip;
            this.pad8.Location = new System.Drawing.Point(134, 6);
            this.pad8.Name = "pad8";
            this.pad8.Click += new System.EventHandler(this.pad_Clicked);
 
            // pad1 
            this.pad1.ContextMenuStrip = this.padContextMenuStrip;
            this.pad1.Location = new System.Drawing.Point(9, 256);
            this.pad1.Name = "pad1";
            this.pad1.Click += new System.EventHandler(this.pad_Clicked);
          
            // pad4
            this.pad4.ContextMenuStrip = this.padContextMenuStrip;
            this.pad4.Location = new System.Drawing.Point(9, 131);
            this.pad4.Name = "pad4";
            this.pad4.Click += new System.EventHandler(this.pad_Clicked);
          
            // pad7
            this.pad7.ContextMenuStrip = this.padContextMenuStrip;
            this.pad7.Location = new System.Drawing.Point(9, 6);
            this.pad7.Name = "pad7";
            this.pad7.Click += new System.EventHandler(this.pad_Clicked);

            this.selectedPad = this.pad1;  //default selection
            SetPictureBoxImage(selectedPad, imgPadBlackSelected);
         }


        private void InitializePlayers()
        {
            this.player1 = new AudioSampler.Player();
            this.player2 = new AudioSampler.Player();
            this.player3 = new AudioSampler.Player();
            this.player4 = new AudioSampler.Player();
            this.player5 = new AudioSampler.Player();
            this.player6 = new AudioSampler.Player();
            this.player7 = new AudioSampler.Player();
            this.player8 = new AudioSampler.Player();
            this.player9 = new AudioSampler.Player();

            //load the playerList
            playerList = new List<Player>()
            {
            this.player1,
            this.player2,
            this.player3,
            this.player4,
            this.player5,
            this.player6,
            this.player7,
            this.player8,
            this.player9
            };
        }
        //if any of the parameter values change, enable the apply button
        //(visually reminds user that they haven't saved the changes
        private void Echo_ParamsChanged(object sender, EventArgs e)
        {
            btnEcho.Enabled = true;
        }
        private void Chorus_ParamsChanged(object sender, EventArgs e)
        {
            btnChorus.Enabled = true;
        }
        private void Filter_ParamsChanged(object sender, EventArgs e)
        {
            btnFilter.Enabled = true;
        }
        private void Drive_ParamsChanged(object sender, EventArgs e)
        {
            btnDrive.Enabled = true;
        }

    }
}
