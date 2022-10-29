# Cdp.dll Flow

## Receive Message
```
?OnMessageReceived@ProximalConnector@cdp@@UEAAXAEBUEndpoint@shared@@PEBVITransportMessage@2@@Z
```

```
?HandleMessageInternal@InboundDecryptionBucket@cdp@@MEAAXAEBUEndpoint@shared@@$$QEAV?$unique_ptr@$$CBVITransportMessage@cdp@@U?$default_delete@$$CBVITransportMessage@cdp@@@std@@@std@@@Z
```

## App Control

### Handler
```
?DesktopGetHandlerInternal@ComObjectCreator@shared@@CAXAEBV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@_NAEBU_GUID@@PEAPEAX@Z

GUID_6cc9a3fd_2236_44d0_be9e_162e773c73bf : ICDPComAppControlSystemHandler
```

```
?HandleMessage@AppControlFacadeBase@cdp@@UEAAXPEBVITransportMessage@2@@Z
?DeserializeAppControlLaunchUri@AppControlMessage@cdp@@YA?AUAppControlLaunchUriPayload@12@AEAVBigEndianStreamReader@2@@Z
?LaunchUri@AppControlFacadeBase@cdp@@IEAAX_KAEBV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@W4CDPAppLocation@@PEBE001@Z
?HandleUriLaunch@AppControlFacade@cdp@@MEAAX_KAEBUSessionUserIdentity@shared@@AEBV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@W4CDPAppLocation@@PEBE0022@Z
```

```
?get_LaunchUri@KnownRemoteSystemCapabilitiesStatics@RemoteSystems@System@Windows@@UEAAJPEAPEAUHSTRING__@@@Z
?get_NearShare@KnownRemoteSystemCapabilitiesStatics@RemoteSystems@System@Windows@@UEAAJPEAPEAUHSTRING__@@@Z
```

```
cdp.dll!cdp::TransportManager::EncryptAndSignMessage(class std::shared_ptr<class shared::Session> const &,class cdp::ITransportMessage const &)	Unknown
cdp.dll!cdp::TransportManager::InternalSend(class std::unique_ptr<struct cdp::SendQueueItem,struct std::default_delete<struct cdp::SendQueueItem> > &&)	Unknown
cdp.dll!cdp::TransportManager::SendMessageAsync(class std::unique_ptr<class cdp::ITransportMessage,struct std::default_delete<class cdp::ITransportMessage> >,struct shared::MessagePolicy const &)	Unknown
cdp.dll!cdp::TransportManager::SendMessageAsync(class std::unique_ptr<class cdp::ITransportMessage,struct std::default_delete<class cdp::ITransportMessage> >)	Unknown
cdp.dll!cdp::ClientChannelManager::StartChannel(struct CDPTarget const &,unsigned int)	Unknown
cdp.dll!cdp::BinaryClient::Listen(struct CDPTarget const &,class ICDPBinaryClientCallback *)	Unknown
```

```
?HandleControlMessage@ClientBroker@cdp@@AEAAXPEBVITransportMessage@2@@Z
```