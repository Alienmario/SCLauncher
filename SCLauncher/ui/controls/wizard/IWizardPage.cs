namespace SCLauncher.ui.controls.wizard;

/// Interface implemented by controls that wish to receive navigation callbacks.
public interface IWizardPage
{
	void OnAttachedToWizard(WizardNavigator wizard, bool unstacked) {}
	void OnDetachedFromWizard(WizardNavigator wizard, bool stacked) {}
	void OnNextPageRequest(WizardNavigator wizard) {}
	bool OnPrevPageRequest(WizardNavigator wizard) => true;
}