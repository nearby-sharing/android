namespace ShortDev.Microsoft.ConnectedDevices.Messages;

/// <summary>
/// General payload to represent a HRESULT (<see cref="int"/>).
/// </summary>
public sealed class HResultPayload : ICdpPayload<HResultPayload>
{
    public static HResultPayload Parse(ref EndianReader reader)
        => new()
        {
            HResult = reader.ReadInt32()
        };

    public required int HResult { get; init; }

    public void Write(EndianWriter writer)
    {
        writer.Write(HResult);
    }

    // cdp.dll!HResultToString
    public static string HResultToString(int hresult)
    {
        string result; // rax
        string v2; // rax
        bool v3; // zf
        string v4; // rdx

        if (hresult <= -2147219199)
        {
            if (hresult == -2147219199)
                return "E_CDP_CHANNEL_ALREADY_STARTED";
            if (hresult > -2147220735)
            {
                if (hresult > -2147220478)
                {
                    switch (hresult)
                    {
                        case -2147220477:
                            return "E_CDP_CRYPTOINVALIDSIGNATURE";
                        case -2147220476:
                            return "E_CDP_INVALID_CERTIFICATE";
                        case -2147219967:
                            return "E_CDP_FILE_MIGRATION_COPY_FAILURE";
                        case -2147219711:
                            return "E_CDP_TRANSPORT_MANAGER_NOT_INITIALIZED";
                        case -2147219710:
                            return "E_CDP_TRANSPORT_NOT_INITIALIZED";
                        case -2147219709:
                            return "E_CDP_TRANSPORT_DISCONNECTED";
                        case -2147219708:
                            return "E_CDP_TRANSPORT_DISABLED";
                        case -2147219707:
                            return "E_CDP_TRANSPORT_NOT_RUNNING";
                    }
                    v2 = "E_CDP_BIG_ENDIAN_STREAM_STRING_NOT_TERMINATED";
                    v3 = hresult == -2147219455;
                }
                else
                {
                    if (hresult == -2147220478)
                        return "E_CDP_INVALIDCRYPTARG";
                    if (hresult > -2147220729)
                    {
                        switch (hresult)
                        {
                            case -2147220728:
                                return "E_CDP_HTTP_BADREQUEST";
                            case -2147220727:
                                return "E_CDP_HTTP_BADRESPONSE";
                            case -2147220725:
                                return "E_CDP_HTTP_ENTITYTOOLARGE";
                        }
                        v2 = "E_CDP_UNEXPECTEDCRYPTOERROR";
                        v3 = hresult == -2147220479;
                    }
                    else
                    {
                        switch (hresult)
                        {
                            case -2147220729:
                                return "E_CDP_HTTP_BADSECURITY";
                            case -2147220734:
                                return "E_CDP_HTTPSERVICEERROR";
                            case -2147220733:
                                return "E_CDP_HTTP_BADSTATE";
                            case -2147220732:
                                return "E_CDP_HTTP_BADURL";
                            case -2147220731:
                                return "E_CDP_HTTP_CLIENTAUTHERROR";
                        }
                        v2 = "E_CDP_HTTP_SERVERAUTHERROR";
                        v3 = hresult == -2147220730;
                    }
                }
            }
            else
            {
                if (hresult == -2147220735)
                    return "E_CDP_HTTPERROR";
                if (hresult > -2147221245)
                {
                    switch (hresult)
                    {
                        case -2147221244:
                            return "E_CDP_NOT_INITIALIZED";
                        case -2147221243:
                            return "E_CDP_NOT_FOUND";
                        case -2147221242:
                            return "E_CDP_CANCELLED";
                        case -2147221241:
                            return "E_CDP_INVALID_CONFIGURATION";
                        case -2147221240:
                            return "E_CDP_EXPIRED_CONFIGURATION";
                        case -2147221239:
                            return "E_CDP_TIMED_OUT";
                        case -2147221238:
                            return "E_CDP_AUTHREQUIRED";
                        case -2147220991:
                            return "E_CDP_SOCKETERROR";
                    }
                    v2 = "E_CDP_SOCKETERROR_RETRIABLE";
                    v3 = hresult == -2147220988;
                }
                else
                {
                    if (hresult == -2147221245)
                        return "E_CDP_INVALID_STATE";
                    if (hresult > -2147467260)
                    {
                        switch (hresult)
                        {
                            case -2147467259:
                                return "E_FAIL";
                            case -2147418113:
                                return "E_UNEXPECTED";
                            case -2147221247:
                                return "E_CDP_FAILED_TO_START_THREAD";
                        }
                        v2 = "E_CDP_INVALID_DATA";
                        v3 = hresult == -2147221246;
                    }
                    else
                    {
                        switch (hresult)
                        {
                            case -2147467260:
                                return "E_ABORT";
                            case -2147483638:
                                return "E_PENDING";
                            case -2147483622:
                                return "E_APPLICATION_EXITING";
                            case -2147467263:
                                return "E_NOTIMPL";
                            case -2147467262:
                                return "E_NOINTERFACE";
                        }
                        v2 = "E_POINTER";
                        v3 = hresult == -2147467261;
                    }
                }
            }
        }
        else if (hresult <= -2147217918)
        {
            if (hresult == -2147217918)
                return "E_CDP_DEVICE_AUTH_GET_REQUEST_FAILED";
            if (hresult > -2147218426)
            {
                switch (hresult)
                {
                    case -2147218425:
                        return "E_CDP_INTERNET_TIMEOUT";
                    case -2147218417:
                        return "E_CDP_CLOUD_TRANSMISSION_UNAUTHORIZED";
                    case -2147218416:
                        return "E_CDP_INTERNET_CONNECTIONERROR";
                    case -2147218415:
                        return "E_CDP_INTERNET_HOSTUNREACHABLE";
                    case -2147218175:
                        return "E_CDP_MESSAGE_LENGTH_EXCEEDED";
                    case -2147218174:
                        return "E_CDP_FAILED_TO_JOIN";
                    case -2147218173:
                        return "E_CDP_HOST_NOT_RESPONDING";
                    case -2147218172:
                        return "E_CDP_CLIENT_NOT_RESPONDING";
                }
                v2 = "E_CDP_DEVICE_AUTH";
                v3 = hresult == -2147217919;
            }
            else
            {
                if (hresult == -2147218426)
                    return "E_CDP_CLOUD_TRANSMISSION_FAILURE";
                if (hresult > -2147218687)
                {
                    switch (hresult)
                    {
                        case -2147218431:
                            return "E_CDP_TIMED_OUT_CONNECT";
                        case -2147218430:
                            return "E_CDP_ALREADY_CONNECTED";
                        case -2147218429:
                            return "E_CDP_NOT_CONNECTED";
                    }
                    v2 = "E_CDP_CONSOLE_DISCONNECTING";
                    v3 = hresult == -2147218428;
                }
                else
                {
                    switch (hresult)
                    {
                        case -2147218687:
                            return "E_CDP_TIMED_OUT_PRESENCE";
                        case -2147219198:
                            return "E_CDP_CHANNEL_FAILED_TO_START";
                        case -2147219197:
                            return "E_CDP_MAXIMUM_CHANNELS_STARTED";
                        case -2147218943:
                            return "E_CDP_JNI_CLASS_NOT_FOUND";
                        case -2147218942:
                            return "E_CDP_JNI_METHOD_NOT_FOUND";
                    }
                    v2 = "E_CDP_JNI_RUNTIME_ERROR";
                    v3 = hresult == -2147218941;
                }
            }
        }
        else
        {
            if (hresult > -2147215613)
            {
                switch (hresult)
                {
                    case -2147215612:
                        return "E_CDP_BLUETOOTH_STATEUNKNOWN";
                    case -2147215611:
                        return "E_CDP_BLUETOOTH_ERROR_UNKNOWN";
                    case -2147024891:
                        return "E_ACCESSDENIED";
                    case -2147024890:
                        return "E_HANDLE";
                    case -2147024882:
                        return "E_OUTOFMEMORY";
                    case -2147024809:
                        return "E_INVALIDARG";
                    case -2147024774:
                        return "E_NOT_SUFFICIENT_BUFFER";
                    case 0:
                        return "S_OK";
                }
                result = "S_FALSE";
                if (hresult != 1)
                    return "E_UNKNOWN";
                return result;
            }
            if (hresult == -2147215613)
                return "E_CDP_BLUETOOTH_POWEREDOFF";
            if (hresult > -2147216634)
            {
                switch (hresult)
                {
                    case -2147216633:
                        return "E_CDP_USERIDENTITY_REQUEST_TIMED_OUT";
                    case -2147216632:
                        return "E_CDP_USERIDENTITY_NO_ACCOUNT";
                    case -2147215615:
                        return "E_CDP_BLUETOOTH_UNSUPPORTED";
                }
                v2 = "E_CDP_BLUETOOTH_UNAUTHORIZED";
                v3 = hresult == -2147215614;
            }
            else
            {
                switch (hresult)
                {
                    case -2147216634:
                        return "E_CDP_USERIDENTITY_UNSUPPORTED_SCOPE_ENDPOINT";
                    case -2147216639:
                        return "E_CDP_USERIDENTITY_STABLE_USER_ID_NOT_FOUND";
                    case -2147216638:
                        return "E_CDP_USERIDENTITY_ACCOUNT_ID_NOT_FOUND";
                    case -2147216637:
                        return "E_CDP_USERIDENTITY_USER_SID_NOT_FOUND";
                    case -2147216636:
                        return "E_CDP_USERIDENTITY_ACCOUNT_PROVIDER_TIMED_OUT";
                }
                v2 = "E_CDP_USERIDENTITY_NO_ACCOUNT_PROVIDERS";
                v3 = hresult == -2147216635;
            }
        }
        v4 = "E_UNKNOWN";
        if (v3)
            return v2;
        return v4;
    }

    // cdp.dll!ErrorCodeToString
    public static string ErrorCodeToString(int errorCode)
    {
        string v1; // rdx
        string result; // rax

        if (errorCode > -2147418113)
        {
            switch (errorCode)
            {
                case -2147024891:
                    return "General access denied error";
                case -2147024890:
                    return "Invalid Handle";
                case -2147024882:
                    return "Out of memory";
                case -2147024846:
                    return "Operation is not supported";
                case -2147024809:
                    return "One or more arguments are invalid";
                case -2147024774:
                    return "Insufficient buffer";
                case -2147019873:
                    return "Invalid State";
                default:
                    result = "Success";
                    if (errorCode > 0)
                        return "An unknown error occurred";
                    break;
            }
        }
        else
        {
            switch (errorCode)
            {
                case -2147418113:
                    return "Catastrophic failure";
                case -2147483638:
                    return "Data necessary to complete this operation is not yet available";
                case -2147483637:
                    return "Operation attempted to access data outside the valid range";
                case -2147483634:
                    return "A method was called at an unexpected time";
                case -2147483622:
                    return "The application is exiting and cannot service this request";
                case -2147467263:
                    return "Not Implemented";
                case -2147467262:
                    return "No such interface supported";
                case -2147467261:
                    return "Invalid Pointer";
                case -2147467260:
                    return "Operation aborted";
                default:
                    v1 = "An unknown error occurred";
                    if (errorCode == -2147467259)
                        return "Unspecified error";
                    return v1;
            }
        }
        return result;
    }
}