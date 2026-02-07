// ============================================================================
// Threader Add-in for Autodesk Inventor 2026
// ThreaderForm.Designer.cs - Form Designer (Matches CaseSelection Style)
// ============================================================================

namespace Threader.UI
{
    partial class ThreaderForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            // Form Colors - WHITE THEME (matching CaseSelection)
            System.Drawing.Color bgColor = System.Drawing.Color.FromArgb(250, 250, 250);
            System.Drawing.Color headerColor = System.Drawing.Color.FromArgb(0, 122, 204);
            System.Drawing.Color textColor = System.Drawing.Color.FromArgb(40, 40, 40);
            System.Drawing.Color buttonColor = System.Drawing.Color.FromArgb(0, 122, 204);
            System.Drawing.Color rowHighlight = System.Drawing.Color.FromArgb(180, 255, 180);

            // Initialize all controls
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.pnlMain = new System.Windows.Forms.Panel();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnDone = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            // Cylinder Selection Section
            this.pnlCylinderSection = new System.Windows.Forms.Panel();
            this.lblCylinderHeader = new System.Windows.Forms.Label();
            this.btnCylinderCollapse = new System.Windows.Forms.Label();
            this.pnlSelectCylinder = new System.Windows.Forms.Panel();
            this.picCylinder = new System.Windows.Forms.Panel();
            this.lblSelectCylinder = new System.Windows.Forms.Label();
            this.pnlCylinderDetails = new System.Windows.Forms.Panel();
            this.lblCylinderInfo = new System.Windows.Forms.Label();
            this.lblDiameter = new System.Windows.Forms.Label();
            this.lblLength = new System.Windows.Forms.Label();

            // Thread Selection Section
            this.pnlThreadSection = new System.Windows.Forms.Panel();
            this.lblThreadHeader = new System.Windows.Forms.Label();
            this.btnThreadCollapse = new System.Windows.Forms.Label();
            this.pnlThreadStandard = new System.Windows.Forms.Panel();
            this.lblStandard = new System.Windows.Forms.Label();
            this.cboThreadStandard = new System.Windows.Forms.ComboBox();
            this.pnlThreadDesignation = new System.Windows.Forms.Panel();
            this.lblDesignation = new System.Windows.Forms.Label();
            this.cboThreadDesignation = new System.Windows.Forms.ComboBox();
            this.pnlThreadDetails = new System.Windows.Forms.Panel();
            this.lblThreadInfo = new System.Windows.Forms.Label();
            this.lblPitch = new System.Windows.Forms.Label();
            this.lblMajorDia = new System.Windows.Forms.Label();
            this.lblMinorDia = new System.Windows.Forms.Label();

            // Options Section
            this.pnlOptionsSection = new System.Windows.Forms.Panel();
            this.lblOptionsHeader = new System.Windows.Forms.Label();
            this.btnOptionsCollapse = new System.Windows.Forms.Label();
            this.pnlThreadLength = new System.Windows.Forms.Panel();
            this.lblThreadLength = new System.Windows.Forms.Label();
            this.numThreadLength = new System.Windows.Forms.NumericUpDown();
            this.lblLengthUnit = new System.Windows.Forms.Label();
            this.pnlThreadStart = new System.Windows.Forms.Panel();
            this.chkReverseDirection = new System.Windows.Forms.CheckBox();
            this.pnlHandedness = new System.Windows.Forms.Panel();
            this.chkRightHand = new System.Windows.Forms.CheckBox();
            this.pnlPreview = new System.Windows.Forms.Panel();
            this.chkPreview = new System.Windows.Forms.CheckBox();

            this.SuspendLayout();
            this.pnlHeader.SuspendLayout();
            this.pnlMain.SuspendLayout();
            this.pnlFooter.SuspendLayout();
            this.pnlCylinderSection.SuspendLayout();
            this.pnlSelectCylinder.SuspendLayout();
            this.pnlCylinderDetails.SuspendLayout();
            this.pnlThreadSection.SuspendLayout();
            this.pnlThreadStandard.SuspendLayout();
            this.pnlThreadDesignation.SuspendLayout();
            this.pnlThreadDetails.SuspendLayout();
            this.pnlOptionsSection.SuspendLayout();
            this.pnlThreadLength.SuspendLayout();
            this.pnlThreadStart.SuspendLayout();
            this.pnlHandedness.SuspendLayout();
            this.pnlPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numThreadLength)).BeginInit();

            // =====================================================
            // pnlHeader - Blue header bar
            // =====================================================
            this.pnlHeader.BackColor = headerColor;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Size = new System.Drawing.Size(340, 32);
            this.pnlHeader.Controls.Add(this.btnClose);
            this.pnlHeader.Controls.Add(this.lblTitle);

            this.lblTitle.AutoSize = false;
            this.lblTitle.Location = new System.Drawing.Point(8, 0);
            this.lblTitle.Size = new System.Drawing.Size(240, 32);
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Text = "Threader";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.btnClose.Location = new System.Drawing.Point(308, 0);
            this.btnClose.Size = new System.Drawing.Size(32, 32);
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(232, 17, 35);
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.Text = "×";
            this.btnClose.BackColor = headerColor;
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);

            // =====================================================
            // pnlMain - Main content area
            // =====================================================
            this.pnlMain.BackColor = bgColor;
            this.pnlMain.Location = new System.Drawing.Point(0, 32);
            this.pnlMain.Size = new System.Drawing.Size(340, 540);
            this.pnlMain.AutoScroll = true;
            this.pnlMain.Controls.Add(this.pnlOptionsSection);
            this.pnlMain.Controls.Add(this.pnlThreadSection);
            this.pnlMain.Controls.Add(this.pnlCylinderSection);

            // =====================================================
            // Cylinder Selection Section
            // =====================================================
            this.pnlCylinderSection.BackColor = bgColor;
            this.pnlCylinderSection.Location = new System.Drawing.Point(8, 8);
            this.pnlCylinderSection.Size = new System.Drawing.Size(320, 100);

            this.lblCylinderHeader.AutoSize = false;
            this.lblCylinderHeader.Location = new System.Drawing.Point(0, 0);
            this.lblCylinderHeader.Size = new System.Drawing.Size(240, 22);
            this.lblCylinderHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblCylinderHeader.ForeColor = headerColor;
            this.lblCylinderHeader.Text = "Cylinder Selection";

            this.btnCylinderCollapse.AutoSize = false;
            this.btnCylinderCollapse.Location = new System.Drawing.Point(260, 0);
            this.btnCylinderCollapse.Size = new System.Drawing.Size(20, 22);
            this.btnCylinderCollapse.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnCylinderCollapse.ForeColor = textColor;
            this.btnCylinderCollapse.Text = "∧";
            this.btnCylinderCollapse.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.btnCylinderCollapse.Cursor = System.Windows.Forms.Cursors.Hand;

            // Select Cylinder row
            this.pnlSelectCylinder.BackColor = System.Drawing.Color.Transparent;
            this.pnlSelectCylinder.Location = new System.Drawing.Point(0, 24);
            this.pnlSelectCylinder.Size = new System.Drawing.Size(280, 26);
            this.pnlSelectCylinder.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlSelectCylinder.Controls.Add(this.lblSelectCylinder);
            this.pnlSelectCylinder.Controls.Add(this.picCylinder);
            this.pnlSelectCylinder.Click += new System.EventHandler(this.pnlSelectCylinder_Click);

            this.picCylinder.BackColor = System.Drawing.Color.FromArgb(0, 176, 80);
            this.picCylinder.Location = new System.Drawing.Point(4, 3);
            this.picCylinder.Size = new System.Drawing.Size(20, 20);

            this.lblSelectCylinder.AutoSize = false;
            this.lblSelectCylinder.Location = new System.Drawing.Point(28, 0);
            this.lblSelectCylinder.Size = new System.Drawing.Size(250, 26);
            this.lblSelectCylinder.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSelectCylinder.ForeColor = textColor;
            this.lblSelectCylinder.Text = "Select Cylindrical Face";
            this.lblSelectCylinder.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSelectCylinder.Click += new System.EventHandler(this.pnlSelectCylinder_Click);

            // Cylinder details panel
            this.pnlCylinderDetails.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.pnlCylinderDetails.Location = new System.Drawing.Point(0, 52);
            this.pnlCylinderDetails.Size = new System.Drawing.Size(280, 44);
            this.pnlCylinderDetails.Controls.Add(this.lblCylinderInfo);
            this.pnlCylinderDetails.Controls.Add(this.lblDiameter);
            this.pnlCylinderDetails.Controls.Add(this.lblLength);

            this.lblCylinderInfo.AutoSize = false;
            this.lblCylinderInfo.Location = new System.Drawing.Point(8, 2);
            this.lblCylinderInfo.Size = new System.Drawing.Size(264, 18);
            this.lblCylinderInfo.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.lblCylinderInfo.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.lblCylinderInfo.Text = "No cylinder selected";
            this.lblCylinderInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.lblDiameter.AutoSize = false;
            this.lblDiameter.Location = new System.Drawing.Point(8, 22);
            this.lblDiameter.Size = new System.Drawing.Size(120, 18);
            this.lblDiameter.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblDiameter.ForeColor = textColor;
            this.lblDiameter.Text = "Ø -- mm";
            this.lblDiameter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.lblLength.AutoSize = false;
            this.lblLength.Location = new System.Drawing.Point(140, 22);
            this.lblLength.Size = new System.Drawing.Size(130, 18);
            this.lblLength.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblLength.ForeColor = textColor;
            this.lblLength.Text = "L: -- mm";
            this.lblLength.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.pnlCylinderSection.Controls.Add(this.lblCylinderHeader);
            this.pnlCylinderSection.Controls.Add(this.btnCylinderCollapse);
            this.pnlCylinderSection.Controls.Add(this.pnlSelectCylinder);
            this.pnlCylinderSection.Controls.Add(this.pnlCylinderDetails);

            // =====================================================
            // Thread Selection Section
            // =====================================================
            this.pnlThreadSection.BackColor = bgColor;
            this.pnlThreadSection.Location = new System.Drawing.Point(8, 116);
            this.pnlThreadSection.Size = new System.Drawing.Size(320, 170);

            this.lblThreadHeader.AutoSize = false;
            this.lblThreadHeader.Location = new System.Drawing.Point(0, 0);
            this.lblThreadHeader.Size = new System.Drawing.Size(240, 22);
            this.lblThreadHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblThreadHeader.ForeColor = headerColor;
            this.lblThreadHeader.Text = "Thread Designation";

            this.btnThreadCollapse.AutoSize = false;
            this.btnThreadCollapse.Location = new System.Drawing.Point(260, 0);
            this.btnThreadCollapse.Size = new System.Drawing.Size(20, 22);
            this.btnThreadCollapse.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnThreadCollapse.ForeColor = textColor;
            this.btnThreadCollapse.Text = "∧";
            this.btnThreadCollapse.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.btnThreadCollapse.Cursor = System.Windows.Forms.Cursors.Hand;

            // Thread Standard row (ISO / ANSI)
            this.pnlThreadStandard.BackColor = System.Drawing.Color.Transparent;
            this.pnlThreadStandard.Location = new System.Drawing.Point(0, 24);
            this.pnlThreadStandard.Size = new System.Drawing.Size(280, 30);
            this.pnlThreadStandard.Controls.Add(this.lblStandard);
            this.pnlThreadStandard.Controls.Add(this.cboThreadStandard);

            this.lblStandard.AutoSize = false;
            this.lblStandard.Location = new System.Drawing.Point(4, 0);
            this.lblStandard.Size = new System.Drawing.Size(60, 30);
            this.lblStandard.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblStandard.ForeColor = textColor;
            this.lblStandard.Text = "Standard";
            this.lblStandard.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.cboThreadStandard.Location = new System.Drawing.Point(68, 4);
            this.cboThreadStandard.Size = new System.Drawing.Size(140, 24);
            this.cboThreadStandard.BackColor = System.Drawing.Color.White;
            this.cboThreadStandard.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboThreadStandard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboThreadStandard.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cboThreadStandard.ForeColor = System.Drawing.Color.Black;
            this.cboThreadStandard.Items.AddRange(new object[] { "ISO Metric", "ANSI Unified" });
            this.cboThreadStandard.SelectedIndex = 0;
            this.cboThreadStandard.Enabled = false;
            this.cboThreadStandard.SelectedIndexChanged += new System.EventHandler(this.cboThreadStandard_SelectedIndexChanged);

            // M Size / ANSI Size row
            this.pnlThreadDesignation.BackColor = System.Drawing.Color.Transparent;
            this.pnlThreadDesignation.Location = new System.Drawing.Point(0, 54);
            this.pnlThreadDesignation.Size = new System.Drawing.Size(280, 30);
            this.pnlThreadDesignation.Controls.Add(this.lblDesignation);
            this.pnlThreadDesignation.Controls.Add(this.cboThreadDesignation);

            this.lblDesignation.AutoSize = false;
            this.lblDesignation.Location = new System.Drawing.Point(4, 0);
            this.lblDesignation.Size = new System.Drawing.Size(50, 30);
            this.lblDesignation.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDesignation.ForeColor = textColor;
            this.lblDesignation.Text = "Size";
            this.lblDesignation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.cboThreadDesignation.Location = new System.Drawing.Point(58, 4);
            this.cboThreadDesignation.Size = new System.Drawing.Size(80, 24);
            this.cboThreadDesignation.BackColor = System.Drawing.Color.White;
            this.cboThreadDesignation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboThreadDesignation.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboThreadDesignation.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cboThreadDesignation.ForeColor = System.Drawing.Color.Black;
            this.cboThreadDesignation.Enabled = false;
            this.cboThreadDesignation.SelectedIndexChanged += new System.EventHandler(this.cboThreadDesignation_SelectedIndexChanged);

            // Pitch combo box (new)
            this.lblPitchSelect = new System.Windows.Forms.Label();
            this.cboPitch = new System.Windows.Forms.ComboBox();
            this.pnlThreadDesignation.Controls.Add(this.lblPitchSelect);
            this.pnlThreadDesignation.Controls.Add(this.cboPitch);

            this.lblPitchSelect.AutoSize = false;
            this.lblPitchSelect.Location = new System.Drawing.Point(145, 0);
            this.lblPitchSelect.Size = new System.Drawing.Size(40, 30);
            this.lblPitchSelect.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblPitchSelect.ForeColor = textColor;
            this.lblPitchSelect.Text = "Pitch";
            this.lblPitchSelect.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.cboPitch.Location = new System.Drawing.Point(188, 4);
            this.cboPitch.Size = new System.Drawing.Size(85, 24);
            this.cboPitch.BackColor = System.Drawing.Color.White;
            this.cboPitch.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPitch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboPitch.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cboPitch.ForeColor = System.Drawing.Color.Black;
            this.cboPitch.Enabled = false;
            this.cboPitch.SelectedIndexChanged += new System.EventHandler(this.cboPitch_SelectedIndexChanged);

            // Thread details panel
            this.pnlThreadDetails.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.pnlThreadDetails.Location = new System.Drawing.Point(0, 88);
            this.pnlThreadDetails.Size = new System.Drawing.Size(280, 78);
            this.pnlThreadDetails.Controls.Add(this.lblThreadInfo);
            this.pnlThreadDetails.Controls.Add(this.lblPitch);
            this.pnlThreadDetails.Controls.Add(this.lblMajorDia);
            this.pnlThreadDetails.Controls.Add(this.lblMinorDia);

            this.lblThreadInfo.AutoSize = false;
            this.lblThreadInfo.Location = new System.Drawing.Point(8, 4);
            this.lblThreadInfo.Size = new System.Drawing.Size(264, 18);
            this.lblThreadInfo.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.lblThreadInfo.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.lblThreadInfo.Text = "Select a thread designation";
            this.lblThreadInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.lblPitch.AutoSize = false;
            this.lblPitch.Location = new System.Drawing.Point(8, 24);
            this.lblPitch.Size = new System.Drawing.Size(120, 18);
            this.lblPitch.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblPitch.ForeColor = textColor;
            this.lblPitch.Text = "Pitch: -- mm";
            this.lblPitch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.lblMajorDia.AutoSize = false;
            this.lblMajorDia.Location = new System.Drawing.Point(8, 42);
            this.lblMajorDia.Size = new System.Drawing.Size(130, 18);
            this.lblMajorDia.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMajorDia.ForeColor = textColor;
            this.lblMajorDia.Text = "Major Ø: -- mm";
            this.lblMajorDia.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.lblMinorDia.AutoSize = false;
            this.lblMinorDia.Location = new System.Drawing.Point(140, 42);
            this.lblMinorDia.Size = new System.Drawing.Size(130, 18);
            this.lblMinorDia.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMinorDia.ForeColor = textColor;
            this.lblMinorDia.Text = "Minor Ø: -- mm";
            this.lblMinorDia.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.pnlThreadSection.Controls.Add(this.lblThreadHeader);
            this.pnlThreadSection.Controls.Add(this.btnThreadCollapse);
            this.pnlThreadSection.Controls.Add(this.pnlThreadStandard);
            this.pnlThreadSection.Controls.Add(this.pnlThreadDesignation);
            this.pnlThreadSection.Controls.Add(this.pnlThreadDetails);

            // =====================================================
            // Options Section
            // =====================================================
            this.pnlOptionsSection.BackColor = bgColor;
            this.pnlOptionsSection.Location = new System.Drawing.Point(8, 294);
            this.pnlOptionsSection.Size = new System.Drawing.Size(320, 203);

            this.lblOptionsHeader.AutoSize = false;
            this.lblOptionsHeader.Location = new System.Drawing.Point(0, 0);
            this.lblOptionsHeader.Size = new System.Drawing.Size(240, 22);
            this.lblOptionsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblOptionsHeader.ForeColor = headerColor;
            this.lblOptionsHeader.Text = "Options";

            this.btnOptionsCollapse.AutoSize = false;
            this.btnOptionsCollapse.Location = new System.Drawing.Point(260, 0);
            this.btnOptionsCollapse.Size = new System.Drawing.Size(20, 22);
            this.btnOptionsCollapse.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnOptionsCollapse.ForeColor = textColor;
            this.btnOptionsCollapse.Text = "∧";
            this.btnOptionsCollapse.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.btnOptionsCollapse.Cursor = System.Windows.Forms.Cursors.Hand;

            // Thread length row
            this.pnlThreadLength.BackColor = System.Drawing.Color.Transparent;
            this.pnlThreadLength.Location = new System.Drawing.Point(0, 24);
            this.pnlThreadLength.Size = new System.Drawing.Size(280, 28);
            this.pnlThreadLength.Controls.Add(this.lblThreadLength);
            this.pnlThreadLength.Controls.Add(this.numThreadLength);
            this.pnlThreadLength.Controls.Add(this.lblLengthUnit);

            this.lblThreadLength.AutoSize = false;
            this.lblThreadLength.Location = new System.Drawing.Point(4, 0);
            this.lblThreadLength.Size = new System.Drawing.Size(90, 28);
            this.lblThreadLength.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblThreadLength.ForeColor = textColor;
            this.lblThreadLength.Text = "Thread Length";
            this.lblThreadLength.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.numThreadLength.Location = new System.Drawing.Point(100, 3);
            this.numThreadLength.Size = new System.Drawing.Size(100, 23);
            this.numThreadLength.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numThreadLength.DecimalPlaces = 2;
            this.numThreadLength.Minimum = 0.1M;
            this.numThreadLength.Maximum = 1000M;
            this.numThreadLength.Value = 10M;
            this.numThreadLength.Increment = 0.5M;
            this.numThreadLength.Enabled = false;
            this.numThreadLength.ValueChanged += new System.EventHandler(this.numThreadLength_ValueChanged);

            this.lblLengthUnit.AutoSize = false;
            this.lblLengthUnit.Location = new System.Drawing.Point(205, 0);
            this.lblLengthUnit.Size = new System.Drawing.Size(40, 28);
            this.lblLengthUnit.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblLengthUnit.ForeColor = textColor;
            this.lblLengthUnit.Text = "mm";
            this.lblLengthUnit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Reverse Direction row
            this.pnlThreadStart.BackColor = System.Drawing.Color.Transparent;
            this.pnlThreadStart.Location = new System.Drawing.Point(0, 54);
            this.pnlThreadStart.Size = new System.Drawing.Size(280, 26);
            this.pnlThreadStart.Controls.Add(this.chkReverseDirection);

            this.chkReverseDirection.AutoSize = false;
            this.chkReverseDirection.Location = new System.Drawing.Point(4, 2);
            this.chkReverseDirection.Size = new System.Drawing.Size(200, 22);
            this.chkReverseDirection.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkReverseDirection.ForeColor = textColor;
            this.chkReverseDirection.Text = "Reverse Thread Direction";
            this.chkReverseDirection.Checked = false;
            this.chkReverseDirection.Enabled = false;
            this.chkReverseDirection.CheckedChanged += new System.EventHandler(this.chkReverseDirection_CheckedChanged);

            // Handedness row
            this.pnlHandedness.BackColor = System.Drawing.Color.Transparent;
            this.pnlHandedness.Location = new System.Drawing.Point(0, 80);
            this.pnlHandedness.Size = new System.Drawing.Size(280, 26);
            this.pnlHandedness.Controls.Add(this.chkRightHand);

            this.chkRightHand.AutoSize = false;
            this.chkRightHand.Location = new System.Drawing.Point(4, 2);
            this.chkRightHand.Size = new System.Drawing.Size(200, 22);
            this.chkRightHand.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkRightHand.ForeColor = textColor;
            this.chkRightHand.Text = "Right-Hand Thread";
            this.chkRightHand.Checked = true;
            this.chkRightHand.Enabled = false;
            this.chkRightHand.CheckedChanged += new System.EventHandler(this.chkRightHand_CheckedChanged);

            // Preview row
            this.pnlPreview.BackColor = System.Drawing.Color.Transparent;
            this.pnlPreview.Location = new System.Drawing.Point(0, 106);
            this.pnlPreview.Size = new System.Drawing.Size(280, 26);
            this.pnlPreview.Controls.Add(this.chkPreview);

            this.chkPreview.AutoSize = false;
            this.chkPreview.Location = new System.Drawing.Point(4, 2);
            this.chkPreview.Size = new System.Drawing.Size(200, 22);
            this.chkPreview.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkPreview.ForeColor = textColor;
            this.chkPreview.Text = "Show Preview";
            this.chkPreview.Checked = true;
            this.chkPreview.Enabled = false;
            this.chkPreview.CheckedChanged += new System.EventHandler(this.chkPreview_CheckedChanged);

            // Profile Type row
            this.pnlProfileType = new System.Windows.Forms.Panel();
            this.lblProfileType = new System.Windows.Forms.Label();
            this.cboProfileType = new System.Windows.Forms.ComboBox();

            this.pnlProfileType.BackColor = System.Drawing.Color.Transparent;
            this.pnlProfileType.Location = new System.Drawing.Point(0, 132);
            this.pnlProfileType.Size = new System.Drawing.Size(280, 28);
            this.pnlProfileType.Controls.Add(this.lblProfileType);
            this.pnlProfileType.Controls.Add(this.cboProfileType);

            this.lblProfileType.AutoSize = false;
            this.lblProfileType.Location = new System.Drawing.Point(4, 0);
            this.lblProfileType.Size = new System.Drawing.Size(90, 28);
            this.lblProfileType.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblProfileType.ForeColor = textColor;
            this.lblProfileType.Text = "Profile Type";
            this.lblProfileType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.cboProfileType.Location = new System.Drawing.Point(100, 3);
            this.cboProfileType.Size = new System.Drawing.Size(140, 24);
            this.cboProfileType.BackColor = System.Drawing.Color.White;
            this.cboProfileType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboProfileType.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboProfileType.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cboProfileType.ForeColor = System.Drawing.Color.Black;
            this.cboProfileType.Items.AddRange(new object[] { "Trapezoidal (Acme)", "Triangular (V-60°)", "Square" });
            this.cboProfileType.SelectedIndex = 1;  // Default to Triangular (V-60°)
            this.cboProfileType.Enabled = false;

            // Resize Cylinder row
            this.pnlResizeCylinder = new System.Windows.Forms.Panel();
            this.chkResizeCylinder = new System.Windows.Forms.CheckBox();

            this.pnlResizeCylinder.BackColor = System.Drawing.Color.Transparent;
            this.pnlResizeCylinder.Location = new System.Drawing.Point(0, 160);
            this.pnlResizeCylinder.Size = new System.Drawing.Size(280, 26);
            this.pnlResizeCylinder.Controls.Add(this.chkResizeCylinder);

            this.chkResizeCylinder.AutoSize = false;
            this.chkResizeCylinder.Location = new System.Drawing.Point(4, 2);
            this.chkResizeCylinder.Size = new System.Drawing.Size(260, 22);
            this.chkResizeCylinder.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkResizeCylinder.ForeColor = textColor;
            this.chkResizeCylinder.Text = "Auto-resize to thread major Ø";
            this.chkResizeCylinder.Checked = true;
            this.chkResizeCylinder.Enabled = false;

            this.pnlOptionsSection.Controls.Add(this.lblOptionsHeader);
            this.pnlOptionsSection.Controls.Add(this.btnOptionsCollapse);
            this.pnlOptionsSection.Controls.Add(this.pnlThreadLength);
            this.pnlOptionsSection.Controls.Add(this.pnlThreadStart);
            this.pnlOptionsSection.Controls.Add(this.pnlHandedness);
            this.pnlOptionsSection.Controls.Add(this.pnlPreview);
            this.pnlOptionsSection.Controls.Add(this.pnlProfileType);
            this.pnlOptionsSection.Controls.Add(this.pnlResizeCylinder);

            // =====================================================
            // pnlFooter - Apply/Done/Cancel buttons
            // =====================================================
            this.pnlFooter.BackColor = bgColor;
            this.pnlFooter.Location = new System.Drawing.Point(0, 602);
            this.pnlFooter.Size = new System.Drawing.Size(340, 48);

            this.btnApply.BackColor = buttonColor;
            this.btnApply.Location = new System.Drawing.Point(12, 8);
            this.btnApply.Size = new System.Drawing.Size(80, 32);
            this.btnApply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApply.FlatAppearance.BorderSize = 0;
            this.btnApply.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnApply.ForeColor = System.Drawing.Color.White;
            this.btnApply.Text = "Apply";
            this.btnApply.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnApply.Enabled = false;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);

            this.btnDone.BackColor = buttonColor;
            this.btnDone.Location = new System.Drawing.Point(118, 8);
            this.btnDone.Size = new System.Drawing.Size(80, 32);
            this.btnDone.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDone.FlatAppearance.BorderSize = 0;
            this.btnDone.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnDone.ForeColor = System.Drawing.Color.White;
            this.btnDone.Text = "Done";
            this.btnDone.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDone.Enabled = false;
            this.btnDone.Click += new System.EventHandler(this.btnDone_Click);

            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.btnCancel.Location = new System.Drawing.Point(224, 8);
            this.btnCancel.Size = new System.Drawing.Size(80, 32);
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            this.pnlFooter.Controls.Add(this.btnApply);
            this.pnlFooter.Controls.Add(this.btnDone);
            this.pnlFooter.Controls.Add(this.btnCancel);

            // =====================================================
            // Form Settings
            // =====================================================
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = bgColor;
            this.ClientSize = new System.Drawing.Size(340, 650);
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.pnlFooter);
            this.Controls.Add(this.pnlHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ThreaderForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(192, 100);
            this.Text = "Threader";
            this.TopMost = true;

            this.pnlHeader.ResumeLayout(false);
            this.pnlMain.ResumeLayout(false);
            this.pnlFooter.ResumeLayout(false);
            this.pnlCylinderSection.ResumeLayout(false);
            this.pnlSelectCylinder.ResumeLayout(false);
            this.pnlCylinderDetails.ResumeLayout(false);
            this.pnlThreadSection.ResumeLayout(false);
            this.pnlThreadStandard.ResumeLayout(false);
            this.pnlThreadDesignation.ResumeLayout(false);
            this.pnlThreadDetails.ResumeLayout(false);
            this.pnlOptionsSection.ResumeLayout(false);
            this.pnlThreadLength.ResumeLayout(false);
            this.pnlThreadStart.ResumeLayout(false);
            this.pnlHandedness.ResumeLayout(false);
            this.pnlPreview.ResumeLayout(false);
            this.pnlProfileType.ResumeLayout(false);
            this.pnlResizeCylinder.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numThreadLength)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        // Header
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Button btnClose;

        // Main Content
        private System.Windows.Forms.Panel pnlMain;

        // Footer
        private System.Windows.Forms.Panel pnlFooter;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnDone;
        private System.Windows.Forms.Button btnCancel;

        // Cylinder Section
        private System.Windows.Forms.Panel pnlCylinderSection;
        private System.Windows.Forms.Label lblCylinderHeader;
        private System.Windows.Forms.Label btnCylinderCollapse;
        private System.Windows.Forms.Panel pnlSelectCylinder;
        private System.Windows.Forms.Panel picCylinder;
        private System.Windows.Forms.Label lblSelectCylinder;
        private System.Windows.Forms.Panel pnlCylinderDetails;
        private System.Windows.Forms.Label lblCylinderInfo;
        private System.Windows.Forms.Label lblDiameter;
        private System.Windows.Forms.Label lblLength;

        // Thread Section
        private System.Windows.Forms.Panel pnlThreadSection;
        private System.Windows.Forms.Label lblThreadHeader;
        private System.Windows.Forms.Label btnThreadCollapse;
        private System.Windows.Forms.Panel pnlThreadStandard;
        private System.Windows.Forms.Label lblStandard;
        private System.Windows.Forms.ComboBox cboThreadStandard;
        private System.Windows.Forms.Panel pnlThreadDesignation;
        private System.Windows.Forms.Label lblDesignation;
        private System.Windows.Forms.ComboBox cboThreadDesignation;
        private System.Windows.Forms.Label lblPitchSelect;
        private System.Windows.Forms.ComboBox cboPitch;
        private System.Windows.Forms.Panel pnlThreadDetails;
        private System.Windows.Forms.Label lblThreadInfo;
        private System.Windows.Forms.Label lblPitch;
        private System.Windows.Forms.Label lblMajorDia;
        private System.Windows.Forms.Label lblMinorDia;

        // Options Section
        private System.Windows.Forms.Panel pnlOptionsSection;
        private System.Windows.Forms.Label lblOptionsHeader;
        private System.Windows.Forms.Label btnOptionsCollapse;
        private System.Windows.Forms.Panel pnlThreadLength;
        private System.Windows.Forms.Label lblThreadLength;
        private System.Windows.Forms.NumericUpDown numThreadLength;
        private System.Windows.Forms.Label lblLengthUnit;
        private System.Windows.Forms.Panel pnlThreadStart;
        private System.Windows.Forms.CheckBox chkReverseDirection;
        private System.Windows.Forms.Panel pnlHandedness;
        private System.Windows.Forms.CheckBox chkRightHand;
        private System.Windows.Forms.Panel pnlPreview;
        private System.Windows.Forms.CheckBox chkPreview;
        private System.Windows.Forms.Panel pnlProfileType;
        private System.Windows.Forms.Label lblProfileType;
        private System.Windows.Forms.ComboBox cboProfileType;
        private System.Windows.Forms.Panel pnlResizeCylinder;
        private System.Windows.Forms.CheckBox chkResizeCylinder;
    }
}
