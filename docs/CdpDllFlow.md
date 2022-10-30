# Cdp.dll Flow

## Receive Message
```
?OnMessageReceived@ProximalConnector@cdp@@UEAAXAEBUEndpoint@shared@@PEBVITransportMessage@2@@Z
?HandleMessageInternal@InboundDecryptionBucket@cdp@@MEAAXAEBUEndpoint@shared@@$$QEAV?$unique_ptr@$$CBVITransportMessage@cdp@@U?$default_delete@$$CBVITransportMessage@cdp@@@std@@@std@@@Z
```

```
?OnTransportReceivedDataInternal@TransportManager@cdp@@AEAAXAEBUEndpoint@shared@@AEBV?$vector@EV?$allocator@E@std@@@std@@@Z
?OnTransportReceivedMessageInternal@TransportManager@cdp@@AEAAXAEBUEndpoint@shared@@V?$unique_ptr@VITransportMessage@cdp@@U?$default_delete@VITransportMessage@cdp@@@std@@@std@@@Z
?VerifyAndDecryptMessage@TransportManager@cdp@@AEAA?AV?$unique_ptr@$$CBVITransportMessage@cdp@@U?$default_delete@$$CBVITransportMessage@cdp@@@std@@@std@@AEBV?$shared_ptr@VSession@shared@@@4@AEBVITransportMessage@2@@Z
?HandleSessionMessage@TransportManager@cdp@@AEAAX$$QEAV?$unique_ptr@$$CBVITransportMessage@cdp@@U?$default_delete@$$CBVITransportMessage@cdp@@@std@@@std@@_N@Z
```

## Encryption
```
?VerifyAndDecryptMessage@TransportManager@cdp@@AEAA?AV?$unique_ptr@$$CBVITransportMessage@cdp@@U?$default_delete@$$CBVITransportMessage@cdp@@@std@@@std@@AEBV?$shared_ptr@VSession@shared@@@4@AEBVITransportMessage@2@@Z
?EncryptAndSignMessage@TransportManager@cdp@@AEAA?AV?$unique_ptr@VITransportMessage@cdp@@U?$default_delete@VITransportMessage@cdp@@@std@@@std@@AEBV?$shared_ptr@VSession@shared@@@4@AEBVITransportMessage@2@@Z
```

```
?InternalSend@TransportManager@cdp@@AEAAX$$QEAV?$unique_ptr@USendQueueItem@cdp@@U?$default_delete@USendQueueItem@cdp@@@std@@@std@@@Z
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

## Session

```
?HandleMessage@ClientBroker@cdp@@UEAAXPEBVITransportMessage@2@@Z
?HandleControlMessage@ClientBroker@cdp@@AEAAXPEBVITransportMessage@2@@Z

```

```
cdp.dll!cdp::ClientChannelManager::HandleStartChannelResponse(unsigned __int64,unsigned __int64,enum cdp::ChannelMessage::Result,enum CDPHostSettings)	Unknown
cdp.dll!cdp::ClientBroker::HandleControlMessage(class cdp::ITransportMessage const *)	Unknown
cdp.dll!cdp::ClientBroker::HandleMessage(class cdp::ITransportMessage const *)	Unknown
cdp.dll!cdp::ClientBroker::TransportManagerObserver::OnSessionMessageReceived(class std::unique_ptr<class cdp::ITransportMessage const ,struct std::default_delete<class cdp::ITransportMessage const > > const &)	Unknown
cdp.dll!shared::detail::BaseObservable<shared::Observable<cdp::ITransportManager,cdp::ITransportManagerObserver>,cdp::ITransportManager,cdp::ITransportManagerObserver>::RaiseEventFunctor::operator()()	Unknown
cdp.dll!cdp::OneCoreWorkItemDispatcher::ProcessWorkitems()	Unknown
cdp.dll!cdp::OneCoreWorkItemDispatcher::ThreadPoolProc(struct _TP_CALLBACK_INSTANCE *,void *,struct _TP_WORK *)	Unknown
ntdll.dll!TppWorkpExecuteCallback()	Unknown
ntdll.dll!TppWorkerThread()	Unknown
kernel32.dll!00007ff9ba817034()	Unknown
ntdll.dll!RtlUserThreadStart()	Unknown
```

## NearShare
`cdprt.dll` in `sihost.exe`

## Extract Key

```
?ComputeMessageContext@Message@CryptoPolicy@shared@@SAXPEAVICrypto@3@PEBVIPrivateAsymmetricKey@3@PEBVIPublicAsymmetricKey@3@AEAUKeyHashPair@3@PEAV?$vector@EV?$allocator@E@std@@@std@@@Z
```

```

```

```cpp
*(void**)rax
```
