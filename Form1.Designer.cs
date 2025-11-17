namespace UxPlay_GUI
{
    partial class Visualizer
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
            label1 = new Label();
            label2 = new Label();
            pictureBox1 = new PictureBox();
            label3 = new Label();
            label4 = new Label();
            Album = new Label();
            Artist = new Label();
            composer = new Label();
            genre = new Label();
            Device = new Label();
            label6 = new Label();
            Errors = new Label();
            title = new Label();
            label7 = new Label();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold);
            label1.Location = new Point(12, 428);
            label1.Name = "label1";
            label1.Size = new Size(102, 37);
            label1.TabIndex = 0;
            label1.Text = "Album";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold);
            label2.Location = new Point(12, 464);
            label2.Name = "label2";
            label2.Size = new Size(90, 37);
            label2.TabIndex = 1;
            label2.Text = "Artist";
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(12, 42);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(340, 340);
            pictureBox1.TabIndex = 2;
            pictureBox1.TabStop = false;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold);
            label3.Location = new Point(12, 501);
            label3.Name = "label3";
            label3.Size = new Size(148, 37);
            label3.TabIndex = 3;
            label3.Text = "Composer";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold);
            label4.Location = new Point(12, 538);
            label4.Name = "label4";
            label4.Size = new Size(93, 37);
            label4.TabIndex = 4;
            label4.Text = "Genre";
            // 
            // Album
            // 
            Album.AutoSize = true;
            Album.Font = new Font("Segoe UI", 15.75F);
            Album.Location = new Point(179, 434);
            Album.Name = "Album";
            Album.Size = new Size(51, 30);
            Album.TabIndex = 5;
            Album.Text = "Play";
            // 
            // Artist
            // 
            Artist.AutoSize = true;
            Artist.Font = new Font("Segoe UI", 15.75F);
            Artist.Location = new Point(179, 471);
            Artist.Name = "Artist";
            Artist.Size = new Size(65, 30);
            Artist.TabIndex = 6;
            Artist.Text = "Some";
            // 
            // composer
            // 
            composer.AutoSize = true;
            composer.Font = new Font("Segoe UI", 15.75F);
            composer.Location = new Point(179, 508);
            composer.Name = "composer";
            composer.Size = new Size(68, 30);
            composer.TabIndex = 7;
            composer.Text = "Music";
            // 
            // genre
            // 
            genre.AutoSize = true;
            genre.Font = new Font("Segoe UI", 15.75F);
            genre.Location = new Point(179, 545);
            genre.Name = "genre";
            genre.Size = new Size(51, 30);
            genre.TabIndex = 8;
            genre.Text = "First";
            // 
            // Device
            // 
            Device.AutoSize = true;
            Device.Font = new Font("Microsoft Sans Serif", 9.75F);
            Device.Location = new Point(137, 23);
            Device.Name = "Device";
            Device.Size = new Size(40, 16);
            Device.TabIndex = 10;
            Device.Text = "None";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label6.Location = new Point(13, 23);
            label6.Name = "label6";
            label6.Size = new Size(104, 16);
            label6.TabIndex = 9;
            label6.Text = "Connected To";
            // 
            // Errors
            // 
            Errors.AutoSize = true;
            Errors.Location = new Point(12, 606);
            Errors.Name = "Errors";
            Errors.Size = new Size(56, 15);
            Errors.TabIndex = 11;
            Errors.Text = "No Errors";
            // 
            // title
            // 
            title.AutoSize = true;
            title.Font = new Font("Segoe UI", 15.75F);
            title.Location = new Point(179, 397);
            title.Name = "title";
            title.Size = new Size(72, 30);
            title.TabIndex = 13;
            title.Text = "Please";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold);
            label7.Location = new Point(12, 390);
            label7.Name = "label7";
            label7.Size = new Size(75, 37);
            label7.TabIndex = 12;
            label7.Text = "Title";
            // 
            // Visualizer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(367, 630);
            Controls.Add(title);
            Controls.Add(label7);
            Controls.Add(Errors);
            Controls.Add(Device);
            Controls.Add(label6);
            Controls.Add(genre);
            Controls.Add(composer);
            Controls.Add(Artist);
            Controls.Add(Album);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(pictureBox1);
            Controls.Add(label2);
            Controls.Add(label1);
            Name = "Visualizer";
            Text = "UxPlay Visualizer";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private PictureBox pictureBox1;
        private Label label3;
        private Label label4;
        private Label Album;
        private Label Artist;
        private Label composer;
        private Label genre;
        private Label Device;
        private Label label6;
        private Label Errors;
        private Label title;
        private Label label7;
    }
}
