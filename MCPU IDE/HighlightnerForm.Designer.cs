﻿namespace MCPU.IDE
{
    partial class HighlightnerForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HighlightnerForm));
            this.fctb = new FastColoredTextBoxNS.FastColoredTextBox();
            this.docmap = new FastColoredTextBoxNS.DocumentMap();
            this.ruler1 = new FastColoredTextBoxNS.Ruler();
            ((System.ComponentModel.ISupportInitialize)(this.fctb)).BeginInit();
            this.SuspendLayout();
            // 
            // fctb
            // 
            this.fctb.AutoCompleteBrackets = true;
            this.fctb.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.fctb.AutoScrollMinSize = new System.Drawing.Size(468, 255);
            this.fctb.BackBrush = null;
            this.fctb.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.fctb.CaretColor = System.Drawing.Color.Wheat;
            this.fctb.CharHeight = 17;
            this.fctb.CharWidth = 8;
            this.fctb.CommentPrefix = ";";
            this.fctb.CurrentLineColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.fctb.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.fctb.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.fctb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fctb.FoldingIndicatorColor = System.Drawing.Color.PowderBlue;
            this.fctb.Font = new System.Drawing.Font("Consolas", 11F);
            this.fctb.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.fctb.IndentBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.fctb.IsReplaceMode = false;
            this.fctb.LeftPadding = 17;
            this.fctb.LineNumberColor = System.Drawing.Color.PowderBlue;
            this.fctb.Location = new System.Drawing.Point(0, 24);
            this.fctb.Name = "fctb";
            this.fctb.Paddings = new System.Windows.Forms.Padding(0);
            this.fctb.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.fctb.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("fctb.ServiceColors")));
            this.fctb.Size = new System.Drawing.Size(727, 597);
            this.fctb.TabIndex = 0;
            this.fctb.Text = resources.GetString("fctb.Text");
            this.fctb.TextAreaBorderColor = System.Drawing.Color.PowderBlue;
            this.fctb.Zoom = 100;
            // 
            // docmap
            // 
            this.docmap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.docmap.Dock = System.Windows.Forms.DockStyle.Right;
            this.docmap.ForeColor = System.Drawing.Color.Maroon;
            this.docmap.Location = new System.Drawing.Point(727, 0);
            this.docmap.Name = "docmap";
            this.docmap.Size = new System.Drawing.Size(100, 621);
            this.docmap.TabIndex = 1;
            this.docmap.Target = this.fctb;
            // 
            // ruler1
            // 
            this.ruler1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.ruler1.BackColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.ruler1.CaretTickColor = System.Drawing.Color.Wheat;
            this.ruler1.Cursor = System.Windows.Forms.Cursors.No;
            this.ruler1.Dock = System.Windows.Forms.DockStyle.Top;
            this.ruler1.ForeColor = System.Drawing.Color.PowderBlue;
            this.ruler1.Location = new System.Drawing.Point(0, 0);
            this.ruler1.MaximumSize = new System.Drawing.Size(1073741824, 24);
            this.ruler1.MinimumSize = new System.Drawing.Size(0, 24);
            this.ruler1.Name = "ruler1";
            this.ruler1.Size = new System.Drawing.Size(727, 24);
            this.ruler1.TabIndex = 2;
            this.ruler1.Target = this.fctb;
            // 
            // HighlightnerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.fctb);
            this.Controls.Add(this.ruler1);
            this.Controls.Add(this.docmap);
            this.Name = "HighlightnerForm";
            this.Size = new System.Drawing.Size(827, 621);
            ((System.ComponentModel.ISupportInitialize)(this.fctb)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        public FastColoredTextBoxNS.FastColoredTextBox fctb;
        private FastColoredTextBoxNS.DocumentMap docmap;
        private FastColoredTextBoxNS.Ruler ruler1;
    }
}