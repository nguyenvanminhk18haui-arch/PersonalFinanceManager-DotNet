using System.Windows;
using ThuChiCaNhan.Models;

namespace ThuChiCaNhan;

public partial class CategoryDialog : Window
{
    public CategoryDialog(IEnumerable<ExpenseCategory> categories)
    {
        InitializeComponent();
        CategoriesList.ItemsSource = categories.OrderBy(category => category.Type).ThenBy(category => category.Name).ToList();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
