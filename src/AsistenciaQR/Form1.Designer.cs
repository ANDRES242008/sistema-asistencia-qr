namespace AsistenciaQR
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new Button();
            btnGenerarQR_Click = new Button();
            btnGenerarCarnet_Click = new Button();
            btnAbrirKiosco_Click = new Button();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(12, 12);
            button1.Name = "button1";
            button1.Size = new Size(411, 116);
            button1.TabIndex = 0;
            button1.Text = "CONEXION \r\n";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // btnGenerarQR_Click
            // 
            btnGenerarQR_Click.Location = new Point(468, 12);
            btnGenerarQR_Click.Name = "btnGenerarQR_Click";
            btnGenerarQR_Click.Size = new Size(411, 116);
            btnGenerarQR_Click.TabIndex = 1;
            btnGenerarQR_Click.Text = "Generar QR de prueba";
            btnGenerarQR_Click.UseVisualStyleBackColor = true;
            btnGenerarQR_Click.Click += btnGenerarQR_Click_Click;
            // 
            // btnGenerarCarnet_Click
            // 
            btnGenerarCarnet_Click.Location = new Point(12, 173);
            btnGenerarCarnet_Click.Name = "btnGenerarCarnet_Click";
            btnGenerarCarnet_Click.Size = new Size(411, 114);
            btnGenerarCarnet_Click.TabIndex = 2;
            btnGenerarCarnet_Click.Text = "Generar Carnet";
            btnGenerarCarnet_Click.UseVisualStyleBackColor = true;
            btnGenerarCarnet_Click.Click += btnGenerarCarnet_Click_Click;
            // 
            // btnAbrirKiosco_Click
            // 
            btnAbrirKiosco_Click.Location = new Point(469, 182);
            btnAbrirKiosco_Click.Name = "btnAbrirKiosco_Click";
            btnAbrirKiosco_Click.Size = new Size(411, 108);
            btnAbrirKiosco_Click.TabIndex = 3;
            btnAbrirKiosco_Click.Text = "Abrir Kiosco";
            btnAbrirKiosco_Click.UseVisualStyleBackColor = true;
            btnAbrirKiosco_Click.Click += btnAbrirKiosco_Click_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(926, 685);
            Controls.Add(btnAbrirKiosco_Click);
            Controls.Add(btnGenerarCarnet_Click);
            Controls.Add(btnGenerarQR_Click);
            Controls.Add(button1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
        }

        #endregion

        private Button button1;
        private Button btnGenerarQR_Click;
        private Button btnGenerarCarnet_Click;
        private Button btnAbrirKiosco_Click;
    }
}
