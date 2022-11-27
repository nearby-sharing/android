-- declare protocol
cdp = Proto("mscdp", "Connected Devices Platform")
-- create fields
msglen = ProtoField.uint16("cdp.header.msglen", "MessageLength")
version = ProtoField.uint8("cdp.header.version", "Version")
type = ProtoField.string("cdp.header.type", "Type") -- uint8
flags = ProtoField.uint16("cdp.header.flags", "Flags")
seqid = ProtoField.uint32("cdp.header.seqid", "SequenceNumber")
reqid = ProtoField.uint64("cdp.header.reqid", "RequestID")
fragid = ProtoField.uint16("cdp.header.fragid", "FragmentIndex")
fragcount = ProtoField.uint16("cdp.header.fragcount", "FragmentCount")
sessionid = ProtoField.uint64("cdp.header.sessionid", "SessionID", base.HEX)
channelid = ProtoField.uint64("cdp.header.channelid", "ChannelID")
nextHeader = ProtoField.none("cdp.header.nextheader", "Additional Header")
headerType = ProtoField.string("cdp.header.nextheader.type", "Type") -- uint8
headerSize = ProtoField.uint8("cdp.header.nextheader.size", "Length")
headerValue = ProtoField.bytes("cdp.header.nextheader.value", "Value", base.SPACE)
cdp.fields = {msglen, version, type, flags, seqid, reqid, fragid, fragcount, sessionid, channelid, nextHeader, headerType, headerSize, headerValue}

local data_data = Field.new("data.data")

function TypeToString(type)
    if type == 0 then return "None" end
    if type == 1 then return "Discovery" end
    if type == 2 then return "Connect" end
    if type == 3 then return "Control" end
    if type == 4 then return "Session" end
    if type == 5 then return "Ack" end
    if type == 6 then return "ReliabilityResponse" end
    return "Unknown"
end

function HeaderTypeToString(type)
    if type == 1 then return "ReplyToId" end
    if type == 2 then return "CorrelationVector" end
    if type == 3 then return "WatermarkId" end
    if type == 5 then return "UserMessageRequestId" end
    return type
end

function Read(reader, length)
    local result = reader.range(reader.offset, length)
    reader.offset = reader.offset + length
    return result
end

-- create a function to "postdissect" each frame
function cdp.dissector(buffer, pinfo, tree)
    local bufLen = buffer:len()
    if bufLen == 0 then return end

    local data = data_data().range()
    local reader = { offset = 0, range = data }
    if (data and Read(reader, 2):uint() == 0x3030) then
        pinfo.cols.protocol = cdp.name

        local subtree = tree:add(cdp, "Cdp Header")
        subtree:add(msglen, Read(reader, 2))
        subtree:add(version, Read(reader, 1))
        subtree:add(type, TypeToString(Read(reader, 1):uint()))
        subtree:add(flags, Read(reader, 2))
        subtree:add(seqid, Read(reader, 4))
        subtree:add(reqid, Read(reader, 8))
        subtree:add(fragid, Read(reader, 2))
        subtree:add(fragcount, Read(reader, 2))
        subtree:add(sessionid, Read(reader, 8))
        subtree:add(channelid, Read(reader, 8))

        while true do
            local nextHeaderType = Read(reader, 1):uint()
            local nextHeaderLength = Read(reader, 1):uint()
            if nextHeaderType == 0 then break end

            local nextHeaderTree = subtree:add(nextHeader)
            nextHeaderTree:add(headerType, HeaderTypeToString(nextHeaderType))
            nextHeaderTree:add(headerSize, nextHeaderLength)
            nextHeaderTree:add(headerValue, Read(reader, nextHeaderLength))
        end
    end
end
-- register our protocol as a postdissector
register_postdissector(cdp)
