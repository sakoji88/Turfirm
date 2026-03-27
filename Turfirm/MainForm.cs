using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Turfirm.Services;

namespace Turfirm
{
    public class MainForm : Form
    {
        private readonly CurrentSession _session;
        private readonly TourService _tourService = new TourService();
        private readonly OrderService _orderService = new OrderService();
        private readonly AuthService _authService = new AuthService();
        private readonly List<CartItem> _cart = new List<CartItem>();

        private DataGridView dgvCatalog;
        private DataGridView dgvCart;
        private DataGridView dgvOrders;
        private ComboBox cbDirection;
        private ComboBox cbType;
        private TextBox txtSearch;
        private NumericUpDown nudPrice;
        private NumericUpDown nudQty;
        private CheckBox chkInsurance;
        private CheckBox chkTransfer;
        private ComboBox cbPayment;

        private DataGridView dgvManage;
        private ComboBox cbManageTable;

        private TextBox txtProfileName;
        private TextBox txtProfileEmail;
        private TextBox txtProfilePhone;

        public MainForm(CurrentSession session)
        {
            _session = session;
            Text = $"Turfirm — {_session.FullName} ({_session.Role})";
            Icon = SystemIcons.Application;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.WhiteSmoke;

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(CreateCatalogTab());
            tabs.TabPages.Add(CreateCartTab());
            tabs.TabPages.Add(CreateOrdersTab());
            tabs.TabPages.Add(CreateProfileTab());
            if (_session.Role >= UserRole.Manager)
                tabs.TabPages.Add(CreateManagementTab());

            Controls.Add(tabs);
            LoadCatalog();
            LoadOrders();
        }

        private TabPage CreateCatalogTab()
        {
            var page = new TabPage("Каталог туров") { BackColor = Color.White };
            var pnl = new Panel { Dock = DockStyle.Top, Height = 85 };

            cbDirection = new ComboBox { Left = 15, Top = 15, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cbDirection.Items.AddRange(new[] { "", "Сочи", "Алтай", "Санкт-Петербург", "Байкал" });
            cbDirection.SelectedIndex = 0;
            cbType = new ComboBox { Left = 170, Top = 15, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cbType.Items.AddRange(new[] { "", "Пляжный", "Экскурсионный", "Активный" });
            cbType.SelectedIndex = 0;
            txtSearch = new TextBox { Left = 325, Top = 15, Width = 230 };
            nudPrice = new NumericUpDown { Left = 570, Top = 15, Width = 120, DecimalPlaces = 0, Maximum = 100000, Value = 100000 };
            var btnFilter = new Button { Text = "Найти", Left = 705, Top = 13, Width = 100 };
            btnFilter.Click += (s, e) => LoadCatalog();

            nudQty = new NumericUpDown { Left = 15, Top = 50, Width = 80, Minimum = 1, Maximum = 10, Value = 1 };
            chkInsurance = new CheckBox { Left = 110, Top = 53, Width = 120, Text = "Страховка +8%" };
            chkTransfer = new CheckBox { Left = 240, Top = 53, Width = 170, Text = "Трансфер +50 у.е." };
            var btnAdd = new Button { Text = "В корзину", Left = 425, Top = 47, Width = 120, BackColor = Color.RoyalBlue, ForeColor = Color.White };
            btnAdd.Click += BtnAddToCart_Click;

            pnl.Controls.AddRange(new Control[] { cbDirection, cbType, txtSearch, nudPrice, btnFilter, nudQty, chkInsurance, chkTransfer, btnAdd });
            page.Controls.Add(pnl);

            dgvCatalog = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            dgvCatalog.CellFormatting += DgvCatalog_CellFormatting;
            page.Controls.Add(dgvCatalog);
            return page;
        }

        private TabPage CreateCartTab()
        {
            var page = new TabPage("Корзина") { BackColor = Color.White };
            dgvCart = new DataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true };
            page.Controls.Add(dgvCart);

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 55 };
            cbPayment = new ComboBox { Left = 15, Top = 15, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            cbPayment.Items.AddRange(new[] { "Банковская карта", "Электронный кошелек" });
            cbPayment.SelectedIndex = 0;
            var btnCheckout = new Button { Text = "Оформить заказ", Left = 250, Top = 12, Width = 160, BackColor = Color.SeaGreen, ForeColor = Color.White };
            btnCheckout.Click += BtnCheckout_Click;
            var btnRemove = new Button { Text = "Удалить позицию", Left = 420, Top = 12, Width = 160 };
            btnRemove.Click += BtnRemoveCart_Click;
            footer.Controls.AddRange(new Control[] { cbPayment, btnCheckout, btnRemove });
            page.Controls.Add(footer);
            RefreshCart();
            return page;
        }

        private TabPage CreateOrdersTab()
        {
            var page = new TabPage("Заказы") { BackColor = Color.White };
            dgvOrders = new DataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true };
            page.Controls.Add(dgvOrders);

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 55 };
            var btnPay = new Button { Text = "Оплатить", Left = 15, Top = 12, Width = 130 };
            btnPay.Click += (s, e) => ChangeOrderStatus(pay: true);
            footer.Controls.Add(btnPay);

            if (_session.Role >= UserRole.Manager)
            {
                var btnConfirm = new Button { Text = "Подтвердить (менеджер)", Left = 160, Top = 12, Width = 220, BackColor = Color.DarkOrange, ForeColor = Color.White };
                btnConfirm.Click += (s, e) => ConfirmOrder();
                footer.Controls.Add(btnConfirm);
            }

            page.Controls.Add(footer);
            return page;
        }

        private TabPage CreateProfileTab()
        {
            var page = new TabPage("Личный кабинет") { BackColor = Color.White };
            txtProfileName = new TextBox { Left = 180, Top = 35, Width = 320, Text = _session.FullName };
            txtProfileEmail = new TextBox { Left = 180, Top = 75, Width = 320, Text = _session.Email };
            txtProfilePhone = new TextBox { Left = 180, Top = 115, Width = 320, Text = _session.Phone };
            page.Controls.Add(new Label { Text = "ФИО", Left = 40, Top = 38, Width = 120 });
            page.Controls.Add(new Label { Text = "Email", Left = 40, Top = 78, Width = 120 });
            page.Controls.Add(new Label { Text = "Телефон", Left = 40, Top = 118, Width = 120 });
            page.Controls.AddRange(new Control[] { txtProfileName, txtProfileEmail, txtProfilePhone });
            var btnSave = new Button { Text = "Сохранить", Left = 180, Top = 160, Width = 130, BackColor = Color.MediumSlateBlue, ForeColor = Color.White };
            btnSave.Click += (s, e) =>
            {
                _authService.UpdateProfile(_session, txtProfileName.Text, txtProfileEmail.Text, txtProfilePhone.Text);
                MessageBox.Show("Данные обновлены.");
            };
            page.Controls.Add(btnSave);
            return page;
        }

        private TabPage CreateManagementTab()
        {
            var page = new TabPage(_session.Role == UserRole.Administrator ? "Администрирование" : "Панель менеджера") { BackColor = Color.White };
            var top = new Panel { Dock = DockStyle.Top, Height = 55 };
            cbManageTable = new ComboBox { Left = 15, Top = 15, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            var tables = new List<string> { "Tours", "Guides", "Transports", "AdditionalServices", "Orders", "OrderItems" };
            if (_session.Role == UserRole.Administrator)
                tables.Insert(0, "Users");
            cbManageTable.Items.AddRange(tables.ToArray());
            cbManageTable.SelectedIndex = 0;
            var btnLoad = new Button { Text = "Открыть таблицу", Left = 210, Top = 13, Width = 130 };
            btnLoad.Click += (s, e) => LoadManageTable();
            var btnSave = new Button { Text = "Сохранить изменения", Left = 350, Top = 13, Width = 160, BackColor = Color.SeaGreen, ForeColor = Color.White };
            btnSave.Click += (s, e) => SaveManageRow();
            var btnImage = new Button { Text = "Загрузить фото тура", Left = 520, Top = 13, Width = 160 };
            btnImage.Click += (s, e) => UploadTourImage();
            var btnDelete = new Button { Text = "Удалить", Left = 690, Top = 13, Width = 100, BackColor = Color.IndianRed, ForeColor = Color.White, Enabled = _session.Role == UserRole.Administrator };
            btnDelete.Click += (s, e) => DeleteRow();
            top.Controls.AddRange(new Control[] { cbManageTable, btnLoad, btnSave, btnImage, btnDelete });
            page.Controls.Add(top);

            dgvManage = new DataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            page.Controls.Add(dgvManage);
            LoadManageTable();
            return page;
        }

        private void LoadCatalog()
        {
            dgvCatalog.DataSource = _tourService.GetTours(cbDirection?.Text, cbType?.Text, null, null, nudPrice?.Value, txtSearch?.Text);
        }

        private void DgvCatalog_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvCatalog.Columns[e.ColumnIndex].Name == "BasePrice")
            {
                var row = dgvCatalog.Rows[e.RowIndex];
                var basePrice = Convert.ToDecimal(row.Cells["BasePrice"].Value);
                var oldPriceObj = row.Cells["OldPrice"].Value;
                var discountObj = row.Cells["DiscountPercent"].Value;

                if (basePrice > 1000)
                {
                    row.DefaultCellStyle.BackColor = Color.Moccasin;
                    row.DefaultCellStyle.Font = new Font(dgvCatalog.Font, FontStyle.Bold);
                }

                if (oldPriceObj != DBNull.Value && discountObj != DBNull.Value)
                {
                    var oldPrice = Convert.ToDecimal(oldPriceObj);
                    var discount = Convert.ToInt32(discountObj);
                    var newPrice = oldPrice * (100 - discount) / 100;
                    row.Cells["BasePrice"].Value = $"{newPrice:0.##} (старая {oldPrice:0.##})";
                }
            }
        }

        private void BtnAddToCart_Click(object sender, EventArgs e)
        {
            if (dgvCatalog.CurrentRow == null) return;
            var freeSeats = Convert.ToInt32(dgvCatalog.CurrentRow.Cells["FreeSeats"].Value);
            if (nudQty.Value > freeSeats)
            {
                MessageBox.Show("Количество мест превышает доступный остаток.");
                return;
            }

            _cart.Add(new CartItem
            {
                TourId = Convert.ToInt32(dgvCatalog.CurrentRow.Cells["Id"].Value),
                TourName = dgvCatalog.CurrentRow.Cells["Title"].Value.ToString(),
                BasePrice = decimal.Parse(dgvCatalog.CurrentRow.Cells["BasePrice"].Value.ToString().Split(' ')[0]),
                Quantity = (int)nudQty.Value,
                Insurance = chkInsurance.Checked,
                Transfer = chkTransfer.Checked,
                TransferFee = chkTransfer.Checked ? 50m : 0m
            });
            RefreshCart();
        }

        private void RefreshCart()
        {
            dgvCart.DataSource = null;
            dgvCart.DataSource = _cart.Select(c => new
            {
                c.TourId,
                c.TourName,
                c.Quantity,
                c.BasePrice,
                c.Insurance,
                c.Transfer,
                c.TransferFee,
                Total = c.Total
            }).ToList();
        }

        private void BtnRemoveCart_Click(object sender, EventArgs e)
        {
            if (dgvCart.CurrentRow == null) return;
            var tourId = Convert.ToInt32(dgvCart.CurrentRow.Cells["TourId"].Value);
            var item = _cart.FirstOrDefault(c => c.TourId == tourId);
            if (item != null) _cart.Remove(item);
            RefreshCart();
        }

        private void BtnCheckout_Click(object sender, EventArgs e)
        {
            var orderId = _orderService.CreateOrder(_session.UserId, _cart, cbPayment.Text);
            _cart.Clear();
            RefreshCart();
            LoadCatalog();
            LoadOrders();
            MessageBox.Show($"Заказ №{orderId} создан и зарезервирован на 24 часа.");
        }

        private void LoadOrders()
        {
            dgvOrders.DataSource = _session.Role >= UserRole.Manager ? _orderService.GetOrders() : _orderService.GetOrders(_session.UserId);
        }

        private void ChangeOrderStatus(bool pay)
        {
            if (dgvOrders.CurrentRow == null) return;
            if (!pay) return;
            var id = Convert.ToInt32(dgvOrders.CurrentRow.Cells["Id"].Value);
            _orderService.MarkPaid(id);
            LoadOrders();
        }

        private void ConfirmOrder()
        {
            if (dgvOrders.CurrentRow == null) return;
            var id = Convert.ToInt32(dgvOrders.CurrentRow.Cells["Id"].Value);

            using (var pick = new AssignDialog())
            {
                if (pick.ShowDialog(this) != DialogResult.OK) return;
                _orderService.ConfirmByManager(id, pick.SelectedGuideId, pick.SelectedTransportId);
                LoadOrders();
            }
        }

        private void LoadManageTable()
        {
            dgvManage.DataSource = _tourService.GetAll(cbManageTable.Text);
        }

        private void SaveManageRow()
        {
            if (cbManageTable.Text != "Tours")
            {
                MessageBox.Show("Для краткости демо редактирование доступно для таблицы Tours. Просмотр остальных таблиц — полный.");
                return;
            }
            if (dgvManage.CurrentRow == null) return;
            var rowView = dgvManage.CurrentRow.DataBoundItem as DataRowView;
            if (rowView == null) return;
            _tourService.Upsert("Tours", rowView.Row);
            LoadManageTable();
            LoadCatalog();
        }

        private void DeleteRow()
        {
            if (_session.Role != UserRole.Administrator)
            {
                MessageBox.Show("Удаление доступно только администратору.");
                return;
            }
            if (cbManageTable.Text != "Tours")
            {
                MessageBox.Show("В демо удаление подключено для туров.");
                return;
            }
            if (dgvManage.CurrentRow == null) return;
            var id = Convert.ToInt32(dgvManage.CurrentRow.Cells["Id"].Value);
            _tourService.DeleteTour(id);
            LoadManageTable();
            LoadCatalog();
        }

        private void UploadTourImage()
        {
            if (cbManageTable.Text != "Tours" || dgvManage.CurrentRow == null)
            {
                MessageBox.Show("Сначала откройте таблицу Tours и выберите строку.");
                return;
            }

            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp";
                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                var ext = Path.GetExtension(ofd.FileName);
                var fileName = $"tour_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var imgDir = Path.Combine(appDir, "ProductImages");
                Directory.CreateDirectory(imgDir);
                var destination = Path.Combine(imgDir, fileName);
                File.Copy(ofd.FileName, destination, true);

                dgvManage.CurrentRow.Cells["ImagePath"].Value = Path.Combine("ProductImages", fileName);
                MessageBox.Show("Изображение загружено. Нажмите 'Сохранить изменения'.");
            }
        }
    }
}
