using System;
using System.Windows.Forms;


namespace SharedForms;

public partial class Output : Form
{
	public Output() : base()
	{
		InitializeComponent();
	}


	public void Print(string toPrint)
	{
		string	fixNewLines	=toPrint.ReplaceLineEndings();
		Action<TextBox>	ta	=con => con.AppendText(fixNewLines);
		FormExtensions.Invoke(ConsoleOut, ta);
	}


	public void UpdateProgress(int min, int max, int cur)
	{
		if(cur < min)
		{
			cur	=min;
		}
		else if(cur > max)
		{
			cur	=max;
		}

		Action<ProgressBar>	pmin	=prog => prog.Minimum = min;
		Action<ProgressBar>	pmax	=prog => prog.Maximum = max;
		Action<ProgressBar>	pcur	=prog => prog.Value = cur;

		SharedForms.FormExtensions.Invoke(BuildProgress, pmin);
		SharedForms.FormExtensions.Invoke(BuildProgress, pmax);
		SharedForms.FormExtensions.Invoke(BuildProgress, pcur);
	}
}