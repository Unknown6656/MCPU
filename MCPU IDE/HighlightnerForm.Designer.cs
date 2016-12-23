namespace MCPU.IDE
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
            this.fctb.AutoScrollMinSize = new System.Drawing.Size(251, 204);
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
            this.fctb.LineNumberColor = System.Drawing.Color.PowderBlue;
            this.fctb.Location = new System.Drawing.Point(0, 0);
            this.fctb.Name = "fctb";
            this.fctb.Paddings = new System.Windows.Forms.Padding(0);
            this.fctb.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.fctb.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("fctb.ServiceColors")));
            this.fctb.Size = new System.Drawing.Size(805, 606);
            this.fctb.TabIndex = 0;
            this.fctb.Text = ".inline func topkek ;random\r\n    ret\r\nend func\r\n\r\n    .main\r\n    .kernel\r\n    mov" +
    " [3] 5\r\n    add $8 k[[2]]\r\nlabel:\r\n     call topkek\r\n     syscall -4\r\n     halt";
            this.fctb.TextAreaBorderColor = System.Drawing.Color.PowderBlue;
            this.fctb.Zoom = 100;
            // 
            // HighlightnerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.fctb);
            this.Name = "HighlightnerForm";
            this.Size = new System.Drawing.Size(805, 606);
            ((System.ComponentModel.ISupportInitialize)(this.fctb)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        public FastColoredTextBoxNS.FastColoredTextBox fctb;
    }
}