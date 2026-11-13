using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Store
{
    public partial class Main : Window
    {
        private DbContextOptions<DataContext> DBOptions;
        private string CurrentViewMode = "Transactions";
        private DataGrid CurrentDataGrid;

        public DateTime StartDateFilter { get; set; }
        public DateTime EndDateFilter { get; set; }
        public Main()
        {
            InitializeComponent();
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            optionsBuilder.UseSqlite("Data Source = UserData.db");
            DBOptions = optionsBuilder.Options;

            var now = DateTime.Now;
            StartDateFilter = new DateTime(now.Year, now.Month, 1);
            EndDateFilter = StartDateFilter.AddMonths(1).AddTicks(-1);

            RefreshIdentitySelector();
            LoadTransactionsView();
        }

        private void CbActiveIdentity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentViewMode == "Transactions") LoadTransactionsView();
        }

        public void LoadTransactionsView()
        {
            CurrentViewMode = "Transactions";
            if (CbActiveIdentity.SelectedValue == null)
            {
                MainContentArea.Content = new TextBlock { Text = "Please select a Contragent from the top dropdown.", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                return;
            }
            int selectedIdentityId = (int)CbActiveIdentity.SelectedValue;
            using (var db = new DataContext(DBOptions))
            {
                var transactions = db.Transactions
                    .Include(t => t.Giver)
                    .Include(t => t.Reciever)
                    .Where(t => t.CreationDateTime >= StartDateFilter && t.CreationDateTime <= EndDateFilter)
                    .Where(t => t.Giver.Id == selectedIdentityId || t.Reciever.Id == selectedIdentityId)
                    .ToList();

                CurrentDataGrid = CreateBaseDataGrid();
                CurrentDataGrid.ItemsSource = transactions;
                MainContentArea.Content = CurrentDataGrid;
            }
        }

        private void LoadContragentsView()
        {
            CurrentViewMode = "Contragents";
            using (var db = new DataContext(DBOptions))
            {
                var contragents = db.Contragents.Include(c => c.Account).ToList();
                CurrentDataGrid = CreateBaseDataGrid();
                CurrentDataGrid.ItemsSource = contragents;
                MainContentArea.Content = CurrentDataGrid;
            }
        }
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            OpenFormWindow(null);
        }
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDataGrid?.SelectedItem == null) return;
            OpenFormWindow(CurrentDataGrid.SelectedItem);
        }
        private void BtnDeleteMark_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDataGrid?.SelectedItem == null) return;
            ExecuteMarkForDeletion(CurrentDataGrid.SelectedItem);
        }

        private void ExecuteMarkForDeletion(object targetEntity)
        {
            using (var db = new DataContext(DBOptions))
            {
                if (targetEntity is Transaction trans)
                {
                    var entity = db.Transactions
                        .Include(t => t.Giver).ThenInclude(c => c.Account)
                        .Include(t => t.Reciever).ThenInclude(c => c.Account)
                        .FirstOrDefault(t => t.Id == trans.Id);
                    if (entity != null)
                    {
                        if (entity.Giver != null && entity.Giver.Account != null)
                        {
                            entity.Giver.Account.Amount += entity.Amount;
                        }
                        if (entity.Reciever != null && entity.Reciever.Account != null)
                        {
                            entity.Reciever.Account.Amount -= entity.Amount;
                        }
                        db.Transactions.Remove(entity);
                    }
                }
                else if (targetEntity is Contragent contra)
                {
                    var entity = db.Contragents.Find(contra.Id);
                    if (entity != null)
                    {
                        db.Contragents.Remove(entity);
                    }
                }
                db.SaveChanges();
            }
            RefreshActiveView();
        }
        private void RefreshIdentitySelector()
        {
            using (var db = new DataContext(DBOptions))
            {
                var identities = db.Contragents.ToList();
                CbActiveIdentity.ItemsSource = identities;
                if (identities.Count > 0 && CbActiveIdentity.SelectedIndex == -1)
                {
                    CbActiveIdentity.SelectedIndex = 0;
                }
            }
        }
        private void OpenFormWindow(object targetDataEntity)
        {
            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var formContainer = new StackPanel { Margin = new Thickness(25)};
            scrollViewer.Content = formContainer;

            formContainer.Children.Add(new TextBlock
            {
                Text = $"{(targetDataEntity == null ? "Create new " : "Edit ")} {CurrentViewMode}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = Brushes.DarkSlateGray
            });

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 20, 0, 0) };
            var btnSave = new Button { Content = "Save changes", Width = 120, Height = 35, Background = Brushes.Green, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold };
            var btnCancel = new Button { Content = "Cancel", Width = 100, Height = 35, Margin = new Thickness(10, 0, 0, 0) };

            buttonPanel.Children.Add(btnSave);
            buttonPanel.Children.Add(btnCancel);
            if (targetDataEntity != null)
            {
                var btnFormDelete = new Button { Content = "Delete", Width = 70, Height = 35, Margin = new Thickness(10, 0, 0, 0), Background = Brushes.Crimson, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold };
                btnFormDelete.Click += (s, ev) => {
                    ExecuteMarkForDeletion(targetDataEntity);
                    RefreshActiveView();
                };
                buttonPanel.Children.Add(btnFormDelete);
            }

            using (var db = new DataContext(DBOptions))
            {
                if (CurrentViewMode == "Contragents")
                {
                    var contraObj = (targetDataEntity as Contragent) ?? new Contragent();
                    var txtName = new TextBox { Text = contraObj.Name, Height = 32 };
                    var txtLocation = new TextBox { Text = contraObj.Location, Height = 32 };

                    AddFieldRow(formContainer, "Name:", txtName);
                    AddFieldRow(formContainer, "Location:", txtLocation);

                    btnSave.Click += (s, ev) => {
                        using (var db2 = new DataContext(DBOptions))
                        {
                            contraObj.Name = txtName.Text;
                            contraObj.Location = txtLocation.Text;
                            if (contraObj.Account == null) contraObj.Account = new Safe { Amount = 0 };

                            if (contraObj.Id == 0) db2.Contragents.Add(contraObj); else db2.Contragents.Update(contraObj);
                            db2.SaveChanges();
                        }
                        RefreshIdentitySelector();
                        LoadContragentsView();
                    };
                }
                else if (CurrentViewMode == "Transactions")
                {
                    var transObj = (targetDataEntity as Transaction) ?? new Transaction { CreationDateTime = DateTime.Now };
                    var txtAmount = new TextBox { Text = transObj.Amount.ToString(), Height = 32 };
                    var txtComment = new TextBox { Text = transObj.Comment, Height = 32 };

                    var rbIsGiver = new RadioButton { Content = "Sending", IsChecked = true, Margin = new Thickness(0, 0, 0, 5) };
                    var rbIsReciever = new RadioButton { Content = "Receiving", Margin = new Thickness(0, 0, 0, 10) };

                    if (targetDataEntity != null && CbActiveIdentity.SelectedValue != null)
                    {
                        int currentId = (int)CbActiveIdentity.SelectedValue;
                        if (transObj.Reciever?.Id == currentId)
                        {
                            rbIsReciever.IsChecked = true;
                            rbIsGiver.IsChecked = false;
                        }
                    }

                    var cbOtherParty = new ComboBox { DisplayMemberPath = "Name", SelectedValuePath = "Id", Height = 32 };
                    cbOtherParty.ItemsSource = db.Contragents.ToList();

                    if (targetDataEntity != null)
                    {
                        cbOtherParty.SelectedValue = rbIsGiver.IsChecked == true ? transObj.Reciever?.Id : transObj.Giver?.Id;
                    }

                    formContainer.Children.Add(rbIsGiver);
                    formContainer.Children.Add(rbIsReciever);
                    AddFieldRow(formContainer, "Contragent:", cbOtherParty);
                    AddFieldRow(formContainer, "Transfer Value Amount:", txtAmount);
                    AddFieldRow(formContainer, "Comment:", txtComment);

                    btnSave.Click += (s, ev) =>
                    {
                        if (CbActiveIdentity.SelectedValue == null)
                        {
                            MessageBox.Show("Choose your contragent before saving.", "Missing acting contragent", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        if (cbOtherParty.SelectedValue == null)
                        {
                            MessageBox.Show("Choose a contragent before saving.", "Missing contragent", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        using (var db2 = new DataContext(DBOptions))
                        {
                            double.TryParse(txtAmount.Text, out double amt);
                            int myId = (int)CbActiveIdentity.SelectedValue;
                            int otherId = (int)cbOtherParty.SelectedValue;
                            var Giver = db2.Contragents
                                 .Include(c => c.Account)
                                 .FirstOrDefault(c => c.Id == (rbIsGiver.IsChecked == true ? myId : otherId));
                            var Reciever = db2.Contragents
                                 .Include(c => c.Account)
                                 .FirstOrDefault(c => c.Id == (rbIsGiver.IsChecked == true ? otherId : myId));
                            if (Giver.Account == null) Giver.Account = new Safe { Amount = 0 };
                            if (Reciever.Account == null) Reciever.Account = new Safe { Amount = 0 };

                            if (transObj.Id == 0)
                            {
                                transObj.Amount = amt;
                                transObj.Comment = txtComment.Text;
                                transObj.Giver = Giver;
                                transObj.Reciever = Reciever;
                                transObj.RedactionDateTime = DateTime.Now;
                                Giver.Account.Amount -= amt;
                                Reciever.Account.Amount += amt;

                                db2.Transactions.Add(transObj);
                            }
                            else
                            {
                                var existingTrans = db2.Transactions.Find(transObj.Id);
                                if (existingTrans != null)
                                {
                                    Giver.Account.Amount = Giver.Account.Amount + existingTrans.Amount - amt;
                                    Reciever.Account.Amount = Reciever.Account.Amount - existingTrans.Amount + amt;
                                    existingTrans.Amount = amt;
                                    existingTrans.Comment = txtComment.Text;
                                    existingTrans.Giver = Giver;
                                    existingTrans.Reciever = Reciever;
                                    existingTrans.RedactionDateTime = DateTime.Now;
                                    db2.Transactions.Update(existingTrans);
                                }
                            }
                            db2.SaveChanges();
                        }
                        LoadTransactionsView();
                    };
                }
            }
            btnCancel.Click += (s, ev) => RefreshActiveView();
            formContainer.Children.Add(buttonPanel);
            MainContentArea.Content = scrollViewer;
        }
        private void AddFieldRow(StackPanel parent, string labelText, Control inputControl)
        {
            parent.Children.Add(new TextBlock { Text = labelText, FontWeight = FontWeights.Medium, Foreground = Brushes.SlateGray });
            parent.Children.Add(inputControl);
        }
        private void RefreshActiveView()
        {
            if (CurrentViewMode == "Transactions") LoadTransactionsView();
            else if (CurrentViewMode == "Contragents") LoadContragentsView();
        }
        private void BtnContragents_Click(object sender, RoutedEventArgs e) => LoadContragentsView();
        private void BtnTransactions_Click(object sender, RoutedEventArgs e) => LoadTransactionsView();
        private void BtnFilters_Click(object sender, RoutedEventArgs e) {
            FilterWindow filterWin = new FilterWindow(this);
            if (filterWin.ShowDialog() == true)
            {
                LoadTransactionsView();
            }
        }
        private DataGrid CreateBaseDataGrid()
        {
            var grid = new DataGrid
            {
                AutoGenerateColumns = true,
                IsReadOnly = true,
                Background = Brushes.White,
                RowHeight = 35,
                FontSize = 14
            };

            grid.AutoGeneratingColumn += (s, e) =>
            {
                if (e.PropertyType == typeof(Contragent))
                {
                    e.Cancel = true;
                    var contragentNameColumn = new DataGridTextColumn
                    {
                        Header = $"{e.PropertyName}",
                        Binding = new Binding($"{e.PropertyName}.Name")
                    };
                    grid.Columns.Add(contragentNameColumn);
                }
                else if (e.PropertyType == typeof(Safe))
                {
                    e.Cancel = true;
                    var safeAmountColumn = new DataGridTextColumn
                    {
                        Header = "Account Balance",
                        Binding = new Binding($"{e.PropertyName}.Amount")
                    };
                    grid.Columns.Add(safeAmountColumn);
                }
            };

            return grid;
        }
    }
}
