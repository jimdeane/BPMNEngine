const vscode = require('vscode');

function activate(context) {
  let disposable = vscode.commands.registerCommand('storyforge.helloWorld', function () {
    vscode.window.showInformationMessage('Hello from StoryForge!');
  });

  context.subscriptions.push(disposable);
}

function deactivate() {}

module.exports = {
  activate,
  deactivate
};