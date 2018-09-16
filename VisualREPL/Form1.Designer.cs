namespace VisualREPL
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.scriptInputBox = new System.Windows.Forms.RichTextBox();
            this.consoleTextBox = new System.Windows.Forms.TextBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.runButton = new System.Windows.Forms.Button();
            this.loadFileButton = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.statusLabel = new System.Windows.Forms.Label();
            this.clearLogButton = new System.Windows.Forms.Button();
            this.saveFileButton = new System.Windows.Forms.Button();
            this.traceCheckbox = new System.Windows.Forms.CheckBox();
            this.showBytecodeCheck = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // scriptInputBox
            // 
            this.scriptInputBox.AcceptsTab = true;
            this.scriptInputBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.scriptInputBox.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.scriptInputBox.DetectUrls = false;
            this.scriptInputBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scriptInputBox.Font = new System.Drawing.Font("Dave", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.scriptInputBox.HideSelection = false;
            this.scriptInputBox.Location = new System.Drawing.Point(0, 0);
            this.scriptInputBox.Name = "scriptInputBox";
            this.scriptInputBox.ShowSelectionMargin = true;
            this.scriptInputBox.Size = new System.Drawing.Size(797, 180);
            this.scriptInputBox.TabIndex = 0;
            this.scriptInputBox.Text = "";
            // 
            // consoleTextBox
            // 
            this.consoleTextBox.AcceptsReturn = true;
            this.consoleTextBox.AcceptsTab = true;
            this.consoleTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.consoleTextBox.Font = new System.Drawing.Font("Dave", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.consoleTextBox.HideSelection = false;
            this.consoleTextBox.Location = new System.Drawing.Point(0, 0);
            this.consoleTextBox.Multiline = true;
            this.consoleTextBox.Name = "consoleTextBox";
            this.consoleTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.consoleTextBox.Size = new System.Drawing.Size(797, 176);
            this.consoleTextBox.TabIndex = 1;
            this.consoleTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.consoleTextBox_KeyPress);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitContainer1.Location = new System.Drawing.Point(-1, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.scriptInputBox);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.consoleTextBox);
            this.splitContainer1.Size = new System.Drawing.Size(801, 368);
            this.splitContainer1.SplitterDistance = 184;
            this.splitContainer1.TabIndex = 2;
            // 
            // runButton
            // 
            this.runButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.runButton.Location = new System.Drawing.Point(713, 415);
            this.runButton.Name = "runButton";
            this.runButton.Size = new System.Drawing.Size(75, 23);
            this.runButton.TabIndex = 3;
            this.runButton.Text = "Run";
            this.runButton.UseVisualStyleBackColor = true;
            this.runButton.Click += new System.EventHandler(this.runButton_Click);
            // 
            // loadFileButton
            // 
            this.loadFileButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.loadFileButton.Location = new System.Drawing.Point(555, 415);
            this.loadFileButton.Name = "loadFileButton";
            this.loadFileButton.Size = new System.Drawing.Size(115, 23);
            this.loadFileButton.TabIndex = 4;
            this.loadFileButton.Text = "Load File...";
            this.loadFileButton.UseVisualStyleBackColor = true;
            this.loadFileButton.Click += new System.EventHandler(this.loadFileButton_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // statusLabel
            // 
            this.statusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.statusLabel.Location = new System.Drawing.Point(1, 369);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(706, 19);
            this.statusLabel.TabIndex = 5;
            this.statusLabel.Text = "Ready";
            // 
            // clearLogButton
            // 
            this.clearLogButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.clearLogButton.Location = new System.Drawing.Point(713, 374);
            this.clearLogButton.Name = "clearLogButton";
            this.clearLogButton.Size = new System.Drawing.Size(75, 23);
            this.clearLogButton.TabIndex = 6;
            this.clearLogButton.Text = "Clear Log";
            this.clearLogButton.UseVisualStyleBackColor = true;
            this.clearLogButton.Click += new System.EventHandler(this.clearLogButton_Click);
            // 
            // saveFileButton
            // 
            this.saveFileButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.saveFileButton.Location = new System.Drawing.Point(434, 415);
            this.saveFileButton.Name = "saveFileButton";
            this.saveFileButton.Size = new System.Drawing.Size(115, 23);
            this.saveFileButton.TabIndex = 7;
            this.saveFileButton.Text = "Save File...";
            this.saveFileButton.UseVisualStyleBackColor = true;
            this.saveFileButton.Click += new System.EventHandler(this.saveFileButton_Click);
            // 
            // traceCheckbox
            // 
            this.traceCheckbox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.traceCheckbox.AutoSize = true;
            this.traceCheckbox.Location = new System.Drawing.Point(13, 421);
            this.traceCheckbox.Name = "traceCheckbox";
            this.traceCheckbox.Size = new System.Drawing.Size(100, 17);
            this.traceCheckbox.TabIndex = 8;
            this.traceCheckbox.Text = "Execution trace";
            this.traceCheckbox.UseVisualStyleBackColor = true;
            // 
            // showBytecodeCheck
            // 
            this.showBytecodeCheck.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.showBytecodeCheck.AutoSize = true;
            this.showBytecodeCheck.Location = new System.Drawing.Point(13, 398);
            this.showBytecodeCheck.Name = "showBytecodeCheck";
            this.showBytecodeCheck.Size = new System.Drawing.Size(100, 17);
            this.showBytecodeCheck.TabIndex = 9;
            this.showBytecodeCheck.Text = "Show bytecode";
            this.showBytecodeCheck.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.showBytecodeCheck);
            this.Controls.Add(this.traceCheckbox);
            this.Controls.Add(this.saveFileButton);
            this.Controls.Add(this.clearLogButton);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.loadFileButton);
            this.Controls.Add(this.runButton);
            this.Controls.Add(this.splitContainer1);
            this.Name = "Form1";
            this.Text = "REPL";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox scriptInputBox;
        private System.Windows.Forms.TextBox consoleTextBox;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button runButton;
        private System.Windows.Forms.Button loadFileButton;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Button clearLogButton;
        private System.Windows.Forms.Button saveFileButton;
        private System.Windows.Forms.CheckBox traceCheckbox;
        private System.Windows.Forms.CheckBox showBytecodeCheck;
    }
}

