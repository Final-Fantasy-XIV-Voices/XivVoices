using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace XivVoices.Services;

public interface IChatMessageProvider : IHostedService;

public class ChatMessageProvider(ILogger _logger, Configuration _configuration, IPlaybackService playbackService, IMessageDispatcher _messageDispatcher, IChatGui _chatGui, IFramework _framework, IClientState _clientState) : PlaybackQueue(MessageSource.ChatMessage, _logger, playbackService, _messageDispatcher, _framework), IChatMessageProvider
{
  public Task StartAsync(CancellationToken cancellationToken)
  {
    QueueStart();
    _chatGui.ChatMessage += OnChatMessage;

    _logger.ServiceLifecycle();
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    QueueStop();
    _chatGui.ChatMessage -= OnChatMessage;

    _logger.ServiceLifecycle();
    return Task.CompletedTask;
  }

  private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString sentence, ref bool isHandled)
  {
    string speaker = "";
    try
    {
      foreach (Payload item in sender.Payloads)
      {
        if (item is PlayerPayload player)
        {
          speaker = player.PlayerName;
          break;
        }

        if (item is TextPayload text && text.Text != null)
        {
          speaker = text.Text;
          break;
        }
      }
    }
    catch { }

    bool allowed = false;
    switch (type)
    {
      case XivChatType.Say:
        allowed = _configuration.ChatSayEnabled;
        break;
      case XivChatType.TellOutgoing:
        if (_clientState.LocalPlayer != null)
          speaker = _clientState.LocalPlayer.Name.ToString();
        allowed = _configuration.ChatTellEnabled;
        break;
      case XivChatType.TellIncoming:
        allowed = _configuration.ChatTellEnabled;
        break;
      case XivChatType.Shout:
      case XivChatType.Yell:
        allowed = _configuration.ChatShoutYellEnabled;
        break;
      case XivChatType.Party:
      case XivChatType.CrossParty:
        allowed = _configuration.ChatPartyEnabled;
        break;
      case XivChatType.Alliance:
        allowed = _configuration.ChatAllianceEnabled;
        break;
      case XivChatType.FreeCompany:
        allowed = _configuration.ChatFreeCompanyEnabled;
        break;
      case XivChatType.CrossLinkShell1:
      case XivChatType.CrossLinkShell2:
      case XivChatType.CrossLinkShell3:
      case XivChatType.CrossLinkShell4:
      case XivChatType.CrossLinkShell5:
      case XivChatType.CrossLinkShell6:
      case XivChatType.CrossLinkShell7:
      case XivChatType.CrossLinkShell8:
      case XivChatType.Ls1:
      case XivChatType.Ls2:
      case XivChatType.Ls3:
      case XivChatType.Ls4:
      case XivChatType.Ls5:
      case XivChatType.Ls6:
      case XivChatType.Ls7:
      case XivChatType.Ls8:
        allowed = _configuration.ChatLinkshellEnabled;
        break;
      case XivChatType.CustomEmote:
      case XivChatType.StandardEmote:
        allowed = _configuration.ChatEmotesEnabled;
        break;
    }

    if (allowed)
    {
      _logger.Debug($"speaker::{speaker} sentence::{sentence}");
      QueueEnabled = _configuration.QueueChatMessages;
      EnqueueMessage(speaker, sentence.ToString());
    }
  }
}
