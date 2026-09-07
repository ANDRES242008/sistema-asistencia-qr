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
            btnAbrirEstudiantes_Click = new Button();
            btnAbrirDashboard_Click = new Button();
            btnAbrirReportes_Click = new Button();
            btnAbrirCarnetsQR_Click = new Button();
            btnCrearAdmin_Click = new Button();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(12, 12);
            button1.Name = "button1";
            button1.Size = new Size(292, 86);
            button1.TabIndex = 0;
            button1.Text = "CONEXION \r\n";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // btnGenerarQR_Click
            // 
            btnGenerarQR_Click.Location = new Point(356, 12);
            btnGenerarQR_Click.Name = "btnGenerarQR_Click";
            btnGenerarQR_Click.Size = new Size(284, 86);
            btnGenerarQR_Click.TabIndex = 1;
            btnGenerarQR_Click.Text = "Generar QR de prueba";
            btnGenerarQR_Click.UseVisualStyleBackColor = true;
            btnGenerarQR_Click.Click += btnGenerarQR_Click_Click;
            // 
            // btnGenerarCarnet_Click
            // 
            btnGenerarCarnet_Click.Location = new Point(12, 140);
            btnGenerarCarnet_Click.Name = "btnGenerarCarnet_Click";
            btnGenerarCarnet_Click.Size = new Size(292, 81);
            btnGenerarCarnet_Click.TabIndex = 2;
            btnGenerarCarnet_Click.Text = "Generar Carnet";
            btnGenerarCarnet_Click.UseVisualStyleBackColor = true;
            btnGenerarCarnet_Click.Click += btnGenerarCarnet_Click_Click;
            // 
            // btnAbrirKiosco_Click
            // 
            btnAbrirKiosco_Click.Location = new Point(356, 140);
            btnAbrirKiosco_Click.Name = "btnAbrirKiosco_Click";
            btnAbrirKiosco_Click.Size = new Size(284, 81);
            btnAbrirKiosco_Click.TabIndex = 3;
            btnAbrirKiosco_Click.Text = "Abrir Kiosco";
            btnAbrirKiosco_Click.UseVisualStyleBackColor = true;
            btnAbrirKiosco_Click.Click += btnAbrirKiosco_Click_Click;
            // 
            // btnAbrirEstudiantes_Click
            // 
            btnAbrirEstudiantes_Click.Location = new Point(12, 261);
            btnAbrirEstudiantes_Click.Name = "btnAbrirEstudiantes_Click";
            btnAbrirEstudiantes_Click.Size = new Size(292, 77);
            btnAbrirEstudiantes_Click.TabIndex = 4;
            btnAbrirEstudiantes_Click.Text = "abrir estudiantes ";
            btnAbrirEstudiantes_Click.UseVisualStyleBackColor = true;
            btnAbrirEstudiantes_Click.Click += btnAbrirEstudiantes_Click_Click;
            // 
            // btnAbrirDashboard_Click
            // 
            btnAbrirDashboard_Click.Location = new Point(356, 261);
            btnAbrirDashboard_Click.Name = "btnAbrirDashboard_Click";
            btnAbrirDashboard_Click.Size = new Size(284, 77);
            btnAbrirDashboard_Click.TabIndex = 5;
            btnAbrirDashboard_Click.Text = "DASHBOARD";
            btnAbrirDashboard_Click.UseVisualStyleBackColor = true;
            btnAbrirDashboard_Click.Click += btnAbrirDashboard_Click_Click;
            // 
            // btnAbrirReportes_Click
            // 
            btnAbrirReportes_Click.Location = new Point(12, 366);
            btnAbrirReportes_Click.Name = "btnAbrirReportes_Click";
            btnAbrirReportes_Click.Size = new Size(292, 89);
            btnAbrirReportes_Click.TabIndex = 6;
            btnAbrirReportes_Click.Text = "Reportes";
            btnAbrirReportes_Click.UseVisualStyleBackColor = true;
            btnAbrirReportes_Click.Click += btnAbrirReportes_Click_Click;
            // 
            // btnAbrirCarnetsQR_Click
            // 
            btnAbrirCarnetsQR_Click.Location = new Point(356, 366);
            btnAbrirCarnetsQR_Click.Name = "btnAbrirCarnetsQR_Click";
            btnAbrirCarnetsQR_Click.Size = new Size(284, 89);
            btnAbrirCarnetsQR_Click.TabIndex = 7;
            btnAbrirCarnetsQR_Click.Text = "CENTROS DE CARNETS ";
            btnAbrirCarnetsQR_Click.UseVisualStyleBackColor = true;
            btnAbrirCarnetsQR_Click.Click += btnAbrirCarnetsQR_Click_Click;
            // 
            // btnCrearAdmin_Click
            // 
            btnCrearAdmin_Click.Location = new Point(12, 491);
            btnCrearAdmin_Click.Name = "btnCrearAdmin_Click";
            btnCrearAdmin_Click.Size = new Size(292, 81);
            btnCrearAdmin_Click.TabIndex = 8;
            btnCrearAdmin_Click.Text = "LOGIN 1 ";
            btnCrearAdmin_Click.UseVisualStyleBackColor = true;
            btnCrearAdmin_Click.Click += btnCrearAdmin_Click_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1283, 685);
            Controls.Add(btnCrearAdmin_Click);
            Controls.Add(btnAbrirCarnetsQR_Click);
            Controls.Add(btnAbrirReportes_Click);
            Controls.Add(btnAbrirDashboard_Click);
            Controls.Add(btnAbrirEstudiantes_Click);
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
        private Button btnAbrirEstudiantes_Click;
        private Button btnAbrirDashboard_Click;
        private Button btnAbrirReportes_Click;
        private Button btnAbrirCarnetsQR_Click;
        private Button btnCrearAdmin_Click;
    }
}
