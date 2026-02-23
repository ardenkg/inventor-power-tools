// ============================================================================
// ColoringTool Add-in for Autodesk Inventor 2026
// ColoringToolForm.Designer.cs - Form Designer with Color Picker
// Blue theme matching CaseSelection
// ============================================================================

namespace ColoringTool.UI
{
    partial class ColoringToolForm
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
            // Form Colors - WHITE THEME
            System.Drawing.Color bgColor = System.Drawing.Color.FromArgb(250, 250, 250); // White/Light gray
            System.Drawing.Color headerColor = System.Drawing.Color.FromArgb(0, 122, 204);  // Blue
            System.Drawing.Color textColor = System.Drawing.Color.FromArgb(40, 40, 40); // Dark text for readability
            System.Drawing.Color buttonColor = System.Drawing.Color.FromArgb(0, 122, 204); // Blue
            System.Drawing.Color rowHighlight = System.Drawing.Color.FromArgb(180, 255, 180);

            // Initialize all controls
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.pnlMain = new System.Windows.Forms.Panel();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.btnDone = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnCloseBottom = new System.Windows.Forms.Button();

            // Objects Section
            this.pnlObjects = new System.Windows.Forms.Panel();
            this.lblObjectsHeader = new System.Windows.Forms.Label();
            this.pnlSelectObjects = new System.Windows.Forms.Panel();
            this.picGreen = new System.Windows.Forms.Panel();
            this.lblSelectObjects = new System.Windows.Forms.Label();
            this.pnlSelectAll = new System.Windows.Forms.Panel();
            this.picGray1 = new System.Windows.Forms.Panel();
            this.lblSelectAll = new System.Windows.Forms.Label();
            this.pnlInvert = new System.Windows.Forms.Panel();
            this.picGray2 = new System.Windows.Forms.Panel();
            this.lblInvert = new System.Windows.Forms.Label();

            // Find Similar Section
            this.pnlFindSimilar = new System.Windows.Forms.Panel();
            this.lblFindSimilarHeader = new System.Windows.Forms.Label();
            this.pnlTemplate = new System.Windows.Forms.Panel();
            this.picOrange = new System.Windows.Forms.Panel();
            this.lblTemplate = new System.Windows.Forms.Label();
            this.pnlFindButtons = new System.Windows.Forms.Panel();
            this.btnFindSimilar = new System.Windows.Forms.Button();
            this.btnAddToMain = new System.Windows.Forms.Button();

            // Find Colors Section
            this.pnlFindColors = new System.Windows.Forms.Panel();
            this.lblFindColorsHeader = new System.Windows.Forms.Label();
            this.btnFindColorsCollapse = new System.Windows.Forms.Label();
            this.pnlSearchColorPicker = new System.Windows.Forms.Panel();
            this.pnlSearchSelectedColor = new System.Windows.Forms.Panel();
            this.lblSearchClickToChange = new System.Windows.Forms.Label();
            this.btnPickSearchColorFromFace = new System.Windows.Forms.Button();
            this.pnlColorFaces = new System.Windows.Forms.Panel();
            this.picPurple = new System.Windows.Forms.Panel();
            this.lblColorFaces = new System.Windows.Forms.Label();
            this.pnlColorButtons = new System.Windows.Forms.Panel();
            this.btnFindColors = new System.Windows.Forms.Button();
            this.btnAddColorToMain = new System.Windows.Forms.Button();

            // Filters Section
            this.pnlFilters = new System.Windows.Forms.Panel();
            this.lblFiltersHeader = new System.Windows.Forms.Label();
            this.pnlTypeFilter = new System.Windows.Forms.Panel();
            this.lblTypeFilter = new System.Windows.Forms.Label();
            this.cboTypeFilter = new System.Windows.Forms.ComboBox();
            this.pnlClear = new System.Windows.Forms.Panel();
            this.picRed = new System.Windows.Forms.Panel();
            this.lblClear = new System.Windows.Forms.Label();

            // Color Picker Section
            this.pnlColorSection = new System.Windows.Forms.Panel();
            this.lblColorHeader = new System.Windows.Forms.Label();
            this.pnlColorPicker = new System.Windows.Forms.Panel();
            this.pnlSelectedColor = new System.Windows.Forms.Panel();
            this.lblClickToChange = new System.Windows.Forms.Label();
            this.btnPickColorFromFace = new System.Windows.Forms.Button();

            this.SuspendLayout();
            this.pnlHeader.SuspendLayout();
            this.pnlMain.SuspendLayout();
            this.pnlFooter.SuspendLayout();
            this.pnlObjects.SuspendLayout();
            this.pnlSelectObjects.SuspendLayout();
            this.pnlSelectAll.SuspendLayout();
            this.pnlInvert.SuspendLayout();
            this.pnlFindSimilar.SuspendLayout();
            this.pnlTemplate.SuspendLayout();
            this.pnlFindButtons.SuspendLayout();
            this.pnlFindColors.SuspendLayout();
            this.pnlSearchColorPicker.SuspendLayout();
            this.pnlColorFaces.SuspendLayout();
            this.pnlColorButtons.SuspendLayout();
            this.pnlFilters.SuspendLayout();
            this.pnlTypeFilter.SuspendLayout();
            this.pnlClear.SuspendLayout();
            this.pnlColorSection.SuspendLayout();
            this.pnlColorPicker.SuspendLayout();
            
            // =====================================================
            // pnlHeader - Header bar (BLUE)
            // =====================================================
            this.pnlHeader.BackColor = headerColor;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Size = new System.Drawing.Size(280, 32);
            this.pnlHeader.Controls.Add(this.btnClose);
            this.pnlHeader.Controls.Add(this.lblTitle);

            this.lblTitle.AutoSize = false;
            this.lblTitle.Location = new System.Drawing.Point(8, 0);
            this.lblTitle.Size = new System.Drawing.Size(220, 32);
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Text = "Coloring Tool";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.btnClose.Location = new System.Drawing.Point(248, 0);
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
            this.pnlMain.Size = new System.Drawing.Size(280, 522);
            this.pnlMain.AutoScroll = true;
            this.pnlMain.Controls.Add(this.pnlColorSection);
            this.pnlMain.Controls.Add(this.pnlFilters);
            this.pnlMain.Controls.Add(this.pnlFindColors);
            this.pnlMain.Controls.Add(this.pnlFindSimilar);
            this.pnlMain.Controls.Add(this.pnlObjects);

            // =====================================================
            // Objects Section
            // =====================================================
            this.pnlObjects.BackColor = bgColor;
            this.pnlObjects.Location = new System.Drawing.Point(8, 8);
            this.pnlObjects.Size = new System.Drawing.Size(260, 110);

            this.lblObjectsHeader.AutoSize = false;
            this.lblObjectsHeader.Location = new System.Drawing.Point(0, 0);
            this.lblObjectsHeader.Size = new System.Drawing.Size(260, 22);
            this.lblObjectsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblObjectsHeader.ForeColor = headerColor;
            this.lblObjectsHeader.Text = "Objects";

            // Select Objects row
            this.pnlSelectObjects.BackColor = rowHighlight;
            this.pnlSelectObjects.Location = new System.Drawing.Point(0, 24);
            this.pnlSelectObjects.Size = new System.Drawing.Size(260, 26);
            this.pnlSelectObjects.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlSelectObjects.Controls.Add(this.lblSelectObjects);
            this.pnlSelectObjects.Controls.Add(this.picGreen);
            this.pnlSelectObjects.Click += new System.EventHandler(this.pnlSelectObjects_Click);

            this.picGreen.BackColor = System.Drawing.Color.FromArgb(0, 176, 80);
            this.picGreen.Location = new System.Drawing.Point(4, 3);
            this.picGreen.Size = new System.Drawing.Size(20, 20);

            this.lblSelectObjects.AutoSize = false;
            this.lblSelectObjects.Location = new System.Drawing.Point(28, 0);
            this.lblSelectObjects.Size = new System.Drawing.Size(230, 26);
            this.lblSelectObjects.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSelectObjects.ForeColor = System.Drawing.Color.Black;
            this.lblSelectObjects.Text = "Select Objects (0)";
            this.lblSelectObjects.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSelectObjects.Click += new System.EventHandler(this.pnlSelectObjects_Click);

            // Select All row
            this.pnlSelectAll.BackColor = System.Drawing.Color.Transparent;
            this.pnlSelectAll.Location = new System.Drawing.Point(0, 52);
            this.pnlSelectAll.Size = new System.Drawing.Size(260, 26);
            this.pnlSelectAll.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlSelectAll.Controls.Add(this.lblSelectAll);
            this.pnlSelectAll.Controls.Add(this.picGray1);
            this.pnlSelectAll.Click += new System.EventHandler(this.pnlSelectAll_Click);

            this.picGray1.BackColor = System.Drawing.Color.Gray;
            this.picGray1.Location = new System.Drawing.Point(4, 3);
            this.picGray1.Size = new System.Drawing.Size(20, 20);

            this.lblSelectAll.AutoSize = false;
            this.lblSelectAll.Location = new System.Drawing.Point(28, 0);
            this.lblSelectAll.Size = new System.Drawing.Size(230, 26);
            this.lblSelectAll.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSelectAll.ForeColor = textColor;
            this.lblSelectAll.Text = "Select All";
            this.lblSelectAll.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSelectAll.Click += new System.EventHandler(this.pnlSelectAll_Click);

            // Invert Selection row
            this.pnlInvert.BackColor = System.Drawing.Color.Transparent;
            this.pnlInvert.Location = new System.Drawing.Point(0, 80);
            this.pnlInvert.Size = new System.Drawing.Size(260, 26);
            this.pnlInvert.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlInvert.Controls.Add(this.lblInvert);
            this.pnlInvert.Controls.Add(this.picGray2);
            this.pnlInvert.Click += new System.EventHandler(this.pnlInvert_Click);

            this.picGray2.BackColor = System.Drawing.Color.Gray;
            this.picGray2.Location = new System.Drawing.Point(4, 3);
            this.picGray2.Size = new System.Drawing.Size(20, 20);

            this.lblInvert.AutoSize = false;
            this.lblInvert.Location = new System.Drawing.Point(28, 0);
            this.lblInvert.Size = new System.Drawing.Size(230, 26);
            this.lblInvert.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblInvert.ForeColor = textColor;
            this.lblInvert.Text = "Invert Selection";
            this.lblInvert.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblInvert.Click += new System.EventHandler(this.pnlInvert_Click);

            this.pnlObjects.Controls.Add(this.lblObjectsHeader);
            this.pnlObjects.Controls.Add(this.pnlSelectObjects);
            this.pnlObjects.Controls.Add(this.pnlSelectAll);
            this.pnlObjects.Controls.Add(this.pnlInvert);

            // =====================================================
            // Find Similar Section
            // =====================================================
            this.pnlFindSimilar.BackColor = bgColor;
            this.pnlFindSimilar.Location = new System.Drawing.Point(8, 122);
            this.pnlFindSimilar.Size = new System.Drawing.Size(260, 100);

            this.lblFindSimilarHeader.AutoSize = false;
            this.lblFindSimilarHeader.Location = new System.Drawing.Point(0, 0);
            this.lblFindSimilarHeader.Size = new System.Drawing.Size(260, 22);
            this.lblFindSimilarHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblFindSimilarHeader.ForeColor = headerColor;
            this.lblFindSimilarHeader.Text = "Find Similar";

            // Template Faces row
            this.pnlTemplate.BackColor = System.Drawing.Color.Transparent;
            this.pnlTemplate.Location = new System.Drawing.Point(0, 24);
            this.pnlTemplate.Size = new System.Drawing.Size(260, 26);
            this.pnlTemplate.Controls.Add(this.lblTemplate);
            this.pnlTemplate.Controls.Add(this.picOrange);
            this.pnlTemplate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlTemplate.Click += new System.EventHandler(this.OnTemplateRowClick);

            this.picOrange.BackColor = System.Drawing.Color.FromArgb(255, 192, 0);
            this.picOrange.Location = new System.Drawing.Point(4, 3);
            this.picOrange.Size = new System.Drawing.Size(20, 20);
            this.picOrange.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picOrange.Click += new System.EventHandler(this.OnTemplateRowClick);

            this.lblTemplate.AutoSize = false;
            this.lblTemplate.Location = new System.Drawing.Point(28, 0);
            this.lblTemplate.Size = new System.Drawing.Size(230, 26);
            this.lblTemplate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblTemplate.ForeColor = textColor;
            this.lblTemplate.Text = "Template Faces (0)";
            this.lblTemplate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblTemplate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblTemplate.Click += new System.EventHandler(this.OnTemplateRowClick);

            // Find Similar buttons (BLUE)
            this.pnlFindButtons.BackColor = System.Drawing.Color.Transparent;
            this.pnlFindButtons.Location = new System.Drawing.Point(0, 54);
            this.pnlFindButtons.Size = new System.Drawing.Size(260, 40);
            this.pnlFindButtons.Controls.Add(this.btnFindSimilar);
            this.pnlFindButtons.Controls.Add(this.btnAddToMain);

            this.btnFindSimilar.BackColor = buttonColor;
            this.btnFindSimilar.Location = new System.Drawing.Point(4, 4);
            this.btnFindSimilar.Size = new System.Drawing.Size(100, 30);
            this.btnFindSimilar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFindSimilar.FlatAppearance.BorderSize = 0;
            this.btnFindSimilar.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnFindSimilar.ForeColor = System.Drawing.Color.White;
            this.btnFindSimilar.Text = "Find Similar";
            this.btnFindSimilar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFindSimilar.Click += new System.EventHandler(this.btnFindSimilar_Click);

            this.btnAddToMain.BackColor = buttonColor;
            this.btnAddToMain.Location = new System.Drawing.Point(110, 4);
            this.btnAddToMain.Size = new System.Drawing.Size(100, 30);
            this.btnAddToMain.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddToMain.FlatAppearance.BorderSize = 0;
            this.btnAddToMain.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnAddToMain.ForeColor = System.Drawing.Color.White;
            this.btnAddToMain.Text = "Add to Main";
            this.btnAddToMain.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAddToMain.Click += new System.EventHandler(this.btnAddToMain_Click);

            this.pnlFindSimilar.Controls.Add(this.lblFindSimilarHeader);
            this.pnlFindSimilar.Controls.Add(this.pnlTemplate);
            this.pnlFindSimilar.Controls.Add(this.pnlFindButtons);

            // =====================================================
            // Find Colors Section
            // =====================================================
            this.pnlFindColors.BackColor = bgColor;
            this.pnlFindColors.Location = new System.Drawing.Point(8, 226);
            this.pnlFindColors.Size = new System.Drawing.Size(260, 130);

            this.lblFindColorsHeader.AutoSize = false;
            this.lblFindColorsHeader.Location = new System.Drawing.Point(0, 0);
            this.lblFindColorsHeader.Size = new System.Drawing.Size(260, 22);
            this.lblFindColorsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblFindColorsHeader.ForeColor = headerColor;
            this.lblFindColorsHeader.Text = "Find Colors";

            // Search color picker row
            this.pnlSearchColorPicker.BackColor = System.Drawing.Color.Transparent;
            this.pnlSearchColorPicker.Location = new System.Drawing.Point(0, 24);
            this.pnlSearchColorPicker.Size = new System.Drawing.Size(260, 32);
            this.pnlSearchColorPicker.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlSearchColorPicker.Controls.Add(this.btnPickSearchColorFromFace);
            this.pnlSearchColorPicker.Controls.Add(this.pnlSearchSelectedColor);
            this.pnlSearchColorPicker.Controls.Add(this.lblSearchClickToChange);
            this.pnlSearchColorPicker.Click += new System.EventHandler(this.pnlSearchColorPicker_Click);

            this.pnlSearchSelectedColor.BackColor = System.Drawing.Color.FromArgb(192, 192, 192);
            this.pnlSearchSelectedColor.Location = new System.Drawing.Point(4, 2);
            this.pnlSearchSelectedColor.Size = new System.Drawing.Size(28, 28);
            this.pnlSearchSelectedColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlSearchSelectedColor.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlSearchSelectedColor.Click += new System.EventHandler(this.pnlSearchColorPicker_Click);

            this.lblSearchClickToChange.AutoSize = false;
            this.lblSearchClickToChange.Location = new System.Drawing.Point(40, 0);
            this.lblSearchClickToChange.Size = new System.Drawing.Size(170, 32);
            this.lblSearchClickToChange.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSearchClickToChange.ForeColor = textColor;
            this.lblSearchClickToChange.Text = "Click to select search color";
            this.lblSearchClickToChange.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSearchClickToChange.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblSearchClickToChange.Click += new System.EventHandler(this.pnlSearchColorPicker_Click);

            // Eye dropper button to pick search color from face
            this.btnPickSearchColorFromFace.Location = new System.Drawing.Point(218, 2);
            this.btnPickSearchColorFromFace.Size = new System.Drawing.Size(38, 28);
            this.btnPickSearchColorFromFace.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPickSearchColorFromFace.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(150, 150, 150);
            this.btnPickSearchColorFromFace.FlatAppearance.BorderSize = 1;
            this.btnPickSearchColorFromFace.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnPickSearchColorFromFace.Text = "Pick";
            this.btnPickSearchColorFromFace.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            this.btnPickSearchColorFromFace.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPickSearchColorFromFace.Click += new System.EventHandler(this.btnPickSearchColorFromFace_Click);

            // Face Colors row (purple)
            this.pnlColorFaces.BackColor = System.Drawing.Color.Transparent;
            this.pnlColorFaces.Location = new System.Drawing.Point(0, 58);
            this.pnlColorFaces.Size = new System.Drawing.Size(260, 26);
            this.pnlColorFaces.Controls.Add(this.lblColorFaces);
            this.pnlColorFaces.Controls.Add(this.picPurple);

            this.picPurple.BackColor = System.Drawing.Color.FromArgb(128, 0, 128);
            this.picPurple.Location = new System.Drawing.Point(4, 3);
            this.picPurple.Size = new System.Drawing.Size(20, 20);

            this.lblColorFaces.AutoSize = false;
            this.lblColorFaces.Location = new System.Drawing.Point(28, 0);
            this.lblColorFaces.Size = new System.Drawing.Size(230, 26);
            this.lblColorFaces.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblColorFaces.ForeColor = textColor;
            this.lblColorFaces.Text = "Face Colors (0)";
            this.lblColorFaces.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Find Colors buttons (BLUE)
            this.pnlColorButtons.BackColor = System.Drawing.Color.Transparent;
            this.pnlColorButtons.Location = new System.Drawing.Point(0, 88);
            this.pnlColorButtons.Size = new System.Drawing.Size(260, 40);
            this.pnlColorButtons.Controls.Add(this.btnFindColors);
            this.pnlColorButtons.Controls.Add(this.btnAddColorToMain);

            this.btnFindColors.BackColor = buttonColor;
            this.btnFindColors.Location = new System.Drawing.Point(4, 4);
            this.btnFindColors.Size = new System.Drawing.Size(100, 30);
            this.btnFindColors.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFindColors.FlatAppearance.BorderSize = 0;
            this.btnFindColors.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnFindColors.ForeColor = System.Drawing.Color.White;
            this.btnFindColors.Text = "Find Colors";
            this.btnFindColors.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFindColors.Click += new System.EventHandler(this.btnFindColors_Click);

            this.btnAddColorToMain.BackColor = buttonColor;
            this.btnAddColorToMain.Location = new System.Drawing.Point(110, 4);
            this.btnAddColorToMain.Size = new System.Drawing.Size(100, 30);
            this.btnAddColorToMain.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddColorToMain.FlatAppearance.BorderSize = 0;
            this.btnAddColorToMain.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnAddColorToMain.ForeColor = System.Drawing.Color.White;
            this.btnAddColorToMain.Text = "Add to Main";
            this.btnAddColorToMain.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAddColorToMain.Click += new System.EventHandler(this.btnAddColorToMain_Click);

            this.pnlFindColors.Controls.Add(this.lblFindColorsHeader);
            this.pnlFindColors.Controls.Add(this.pnlSearchColorPicker);
            this.pnlFindColors.Controls.Add(this.pnlColorFaces);
            this.pnlFindColors.Controls.Add(this.pnlColorButtons);

            // =====================================================
            // Filters Section
            // =====================================================
            this.pnlFilters.BackColor = bgColor;
            this.pnlFilters.Location = new System.Drawing.Point(8, 360);
            this.pnlFilters.Size = new System.Drawing.Size(260, 90);

            this.lblFiltersHeader.AutoSize = false;
            this.lblFiltersHeader.Location = new System.Drawing.Point(0, 0);
            this.lblFiltersHeader.Size = new System.Drawing.Size(260, 22);
            this.lblFiltersHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblFiltersHeader.ForeColor = headerColor;
            this.lblFiltersHeader.Text = "Filters";

            // Type Filter row
            this.pnlTypeFilter.BackColor = System.Drawing.Color.Transparent;
            this.pnlTypeFilter.Location = new System.Drawing.Point(0, 24);
            this.pnlTypeFilter.Size = new System.Drawing.Size(260, 30);
            this.pnlTypeFilter.Controls.Add(this.lblTypeFilter);
            this.pnlTypeFilter.Controls.Add(this.cboTypeFilter);

            this.lblTypeFilter.AutoSize = false;
            this.lblTypeFilter.Location = new System.Drawing.Point(4, 0);
            this.lblTypeFilter.Size = new System.Drawing.Size(70, 30);
            this.lblTypeFilter.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblTypeFilter.ForeColor = textColor;
            this.lblTypeFilter.Text = "Type Filter";
            this.lblTypeFilter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.cboTypeFilter.Location = new System.Drawing.Point(78, 4);
            this.cboTypeFilter.Size = new System.Drawing.Size(170, 24);
            this.cboTypeFilter.BackColor = System.Drawing.Color.White;
            this.cboTypeFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTypeFilter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboTypeFilter.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cboTypeFilter.ForeColor = System.Drawing.Color.Black;
            this.cboTypeFilter.Items.AddRange(new object[] {
                "Single Face",
                "Tangent Faces",
                "Boss/Pocket Faces",
                "Adjacent Faces",
                "Feature Faces",
                "Connected Blend Faces"
            });
            this.cboTypeFilter.SelectedIndex = 0;
            this.cboTypeFilter.SelectedIndexChanged += new System.EventHandler(this.cboTypeFilter_SelectedIndexChanged);

            // Clear Selection row
            this.pnlClear.BackColor = System.Drawing.Color.Transparent;
            this.pnlClear.Location = new System.Drawing.Point(0, 58);
            this.pnlClear.Size = new System.Drawing.Size(260, 26);
            this.pnlClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlClear.Controls.Add(this.lblClear);
            this.pnlClear.Controls.Add(this.picRed);
            this.pnlClear.Click += new System.EventHandler(this.pnlClear_Click);

            this.lblClear.AutoSize = false;
            this.lblClear.Location = new System.Drawing.Point(4, 0);
            this.lblClear.Size = new System.Drawing.Size(200, 26);
            this.lblClear.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblClear.ForeColor = textColor;
            this.lblClear.Text = "Clear Selection";
            this.lblClear.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblClear.Click += new System.EventHandler(this.pnlClear_Click);

            this.picRed.BackColor = System.Drawing.Color.FromArgb(180, 0, 0);
            this.picRed.Location = new System.Drawing.Point(232, 3);
            this.picRed.Size = new System.Drawing.Size(20, 20);

            this.pnlFilters.Controls.Add(this.lblFiltersHeader);
            this.pnlFilters.Controls.Add(this.pnlTypeFilter);
            this.pnlFilters.Controls.Add(this.pnlClear);

            // =====================================================
            // Color Picker Section - Single color square with click to change
            // =====================================================
            this.pnlColorSection.BackColor = bgColor;
            this.pnlColorSection.Location = new System.Drawing.Point(8, 454);
            this.pnlColorSection.Size = new System.Drawing.Size(260, 60);

            this.lblColorHeader.AutoSize = false;
            this.lblColorHeader.Location = new System.Drawing.Point(0, 0);
            this.lblColorHeader.Size = new System.Drawing.Size(260, 22);
            this.lblColorHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblColorHeader.ForeColor = headerColor;
            this.lblColorHeader.Text = "Color to Apply";

            // Color picker row
            this.pnlColorPicker.BackColor = System.Drawing.Color.Transparent;
            this.pnlColorPicker.Location = new System.Drawing.Point(0, 24);
            this.pnlColorPicker.Size = new System.Drawing.Size(260, 32);
            this.pnlColorPicker.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlColorPicker.Controls.Add(this.btnPickColorFromFace);
            this.pnlColorPicker.Controls.Add(this.pnlSelectedColor);
            this.pnlColorPicker.Controls.Add(this.lblClickToChange);
            this.pnlColorPicker.Click += new System.EventHandler(this.pnlColorPicker_Click);

            // The color square - default Inventor grey
            this.pnlSelectedColor.BackColor = System.Drawing.Color.FromArgb(192, 192, 192);
            this.pnlSelectedColor.Location = new System.Drawing.Point(4, 2);
            this.pnlSelectedColor.Size = new System.Drawing.Size(28, 28);
            this.pnlSelectedColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlSelectedColor.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlSelectedColor.Click += new System.EventHandler(this.pnlColorPicker_Click);

            // "Click to change color" label
            this.lblClickToChange.AutoSize = false;
            this.lblClickToChange.Location = new System.Drawing.Point(40, 0);
            this.lblClickToChange.Size = new System.Drawing.Size(170, 32);
            this.lblClickToChange.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblClickToChange.ForeColor = textColor;
            this.lblClickToChange.Text = "Click to change color";
            this.lblClickToChange.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblClickToChange.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblClickToChange.Click += new System.EventHandler(this.pnlColorPicker_Click);

            // Eye dropper button to pick color from face
            this.btnPickColorFromFace.Location = new System.Drawing.Point(218, 2);
            this.btnPickColorFromFace.Size = new System.Drawing.Size(38, 28);
            this.btnPickColorFromFace.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPickColorFromFace.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(150, 150, 150);
            this.btnPickColorFromFace.FlatAppearance.BorderSize = 1;
            this.btnPickColorFromFace.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnPickColorFromFace.Text = "Pick";
            this.btnPickColorFromFace.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            this.btnPickColorFromFace.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPickColorFromFace.Click += new System.EventHandler(this.btnPickColorFromFace_Click);

            this.pnlColorSection.Controls.Add(this.lblColorHeader);
            this.pnlColorSection.Controls.Add(this.pnlColorPicker);

            // =====================================================
            // pnlFooter - Done/Apply/Close buttons (BLUE)
            // =====================================================
            this.pnlFooter.BackColor = bgColor;
            this.pnlFooter.Location = new System.Drawing.Point(0, 554);
            this.pnlFooter.Size = new System.Drawing.Size(280, 48);

            this.btnDone.BackColor = buttonColor;
            this.btnDone.Location = new System.Drawing.Point(10, 8);
            this.btnDone.Size = new System.Drawing.Size(80, 32);
            this.btnDone.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDone.FlatAppearance.BorderSize = 0;
            this.btnDone.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnDone.ForeColor = System.Drawing.Color.White;
            this.btnDone.Text = "Done";
            this.btnDone.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDone.Click += new System.EventHandler(this.btnDone_Click);

            this.btnApply.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);  // Blue for Apply (theme consistent)
            this.btnApply.Location = new System.Drawing.Point(100, 8);
            this.btnApply.Size = new System.Drawing.Size(80, 32);
            this.btnApply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApply.FlatAppearance.BorderSize = 0;
            this.btnApply.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnApply.ForeColor = System.Drawing.Color.White;
            this.btnApply.Text = "Apply";
            this.btnApply.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);

            this.btnCloseBottom.BackColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.btnCloseBottom.Location = new System.Drawing.Point(190, 8);
            this.btnCloseBottom.Size = new System.Drawing.Size(80, 32);
            this.btnCloseBottom.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCloseBottom.FlatAppearance.BorderSize = 0;
            this.btnCloseBottom.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCloseBottom.ForeColor = System.Drawing.Color.White;
            this.btnCloseBottom.Text = "Close";
            this.btnCloseBottom.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCloseBottom.Click += new System.EventHandler(this.btnCloseBottom_Click);

            this.pnlFooter.Controls.Add(this.btnDone);
            this.pnlFooter.Controls.Add(this.btnApply);
            this.pnlFooter.Controls.Add(this.btnCloseBottom);

            // =====================================================
            // Form Settings
            // =====================================================
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = bgColor;
            this.ClientSize = new System.Drawing.Size(280, 602);
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.pnlFooter);
            this.Controls.Add(this.pnlHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ColoringToolForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(192, 100);
            this.Text = "Coloring Tool";
            this.TopMost = true;

            this.pnlHeader.ResumeLayout(false);
            this.pnlMain.ResumeLayout(false);
            this.pnlFooter.ResumeLayout(false);
            this.pnlObjects.ResumeLayout(false);
            this.pnlSelectObjects.ResumeLayout(false);
            this.pnlSelectAll.ResumeLayout(false);
            this.pnlInvert.ResumeLayout(false);
            this.pnlFindSimilar.ResumeLayout(false);
            this.pnlTemplate.ResumeLayout(false);
            this.pnlFindButtons.ResumeLayout(false);
            this.pnlFindColors.ResumeLayout(false);
            this.pnlSearchColorPicker.ResumeLayout(false);
            this.pnlColorFaces.ResumeLayout(false);
            this.pnlColorButtons.ResumeLayout(false);
            this.pnlFilters.ResumeLayout(false);
            this.pnlTypeFilter.ResumeLayout(false);
            this.pnlClear.ResumeLayout(false);
            this.pnlColorSection.ResumeLayout(false);
            this.pnlColorPicker.ResumeLayout(false);
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
        private System.Windows.Forms.Button btnDone;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnCloseBottom;

        // Objects Section
        private System.Windows.Forms.Panel pnlObjects;
        private System.Windows.Forms.Label lblObjectsHeader;
        private System.Windows.Forms.Panel pnlSelectObjects;
        private System.Windows.Forms.Panel picGreen;
        private System.Windows.Forms.Label lblSelectObjects;
        private System.Windows.Forms.Panel pnlSelectAll;
        private System.Windows.Forms.Panel picGray1;
        private System.Windows.Forms.Label lblSelectAll;
        private System.Windows.Forms.Panel pnlInvert;
        private System.Windows.Forms.Panel picGray2;
        private System.Windows.Forms.Label lblInvert;

        // Find Similar Section
        private System.Windows.Forms.Panel pnlFindSimilar;
        private System.Windows.Forms.Label lblFindSimilarHeader;
        private System.Windows.Forms.Panel pnlTemplate;
        private System.Windows.Forms.Panel picOrange;
        private System.Windows.Forms.Label lblTemplate;
        private System.Windows.Forms.Panel pnlFindButtons;
        private System.Windows.Forms.Button btnFindSimilar;
        private System.Windows.Forms.Button btnAddToMain;

        // Find Colors Section
        private System.Windows.Forms.Panel pnlFindColors;
        private System.Windows.Forms.Label lblFindColorsHeader;
        private System.Windows.Forms.Label btnFindColorsCollapse;
        private System.Windows.Forms.Panel pnlSearchColorPicker;
        private System.Windows.Forms.Panel pnlSearchSelectedColor;
        private System.Windows.Forms.Label lblSearchClickToChange;
        private System.Windows.Forms.Button btnPickSearchColorFromFace;
        private System.Windows.Forms.Panel pnlColorFaces;
        private System.Windows.Forms.Panel picPurple;
        private System.Windows.Forms.Label lblColorFaces;
        private System.Windows.Forms.Panel pnlColorButtons;
        private System.Windows.Forms.Button btnFindColors;
        private System.Windows.Forms.Button btnAddColorToMain;

        // Filters Section
        private System.Windows.Forms.Panel pnlFilters;
        private System.Windows.Forms.Label lblFiltersHeader;
        private System.Windows.Forms.Panel pnlTypeFilter;
        private System.Windows.Forms.Label lblTypeFilter;
        private System.Windows.Forms.ComboBox cboTypeFilter;
        private System.Windows.Forms.Panel pnlClear;
        private System.Windows.Forms.Panel picRed;
        private System.Windows.Forms.Label lblClear;

        // Color Picker Section
        private System.Windows.Forms.Panel pnlColorSection;
        private System.Windows.Forms.Label lblColorHeader;
        private System.Windows.Forms.Panel pnlColorPicker;
        private System.Windows.Forms.Panel pnlSelectedColor;
        private System.Windows.Forms.Label lblClickToChange;
        private System.Windows.Forms.Button btnPickColorFromFace;
    }
}
