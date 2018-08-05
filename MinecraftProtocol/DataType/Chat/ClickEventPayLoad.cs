using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.DataType.Chat
{
    /// <summary>
    /// JSON object - Defines an event that occurs when this component is clicked. Contains an action key and a value key. value is internally handled as a String, although it can be any type of JSON primitive.
    ///
    /// open_url
    ///		Opens the given URL in the default web browser. Ignored if the player has opted to disable links in chat; may open a GUI prompting the user if the setting for that is enabled. The link's protocol must be set and must be http or https, for security reasons.
    /// open_file
    ///		Cannot be used within JSON chat. Opens a link to any protocol, but cannot be used in JSON chat for security reasons. Only exists to internally implement links for screenshots.
    /// run_command
    ///		Runs the given command. Not required to be a command - clicking this only causes the client to send the given content as a chat message, so if not prefixed with /, they will say the given text instead. If used in a book GUI, the GUI is closed after clicking.
    /// twitch_user_info
    ///		No longer supported; cannot be used within JSON chat. Only usable in 1.8 and below; twitch support was removed in 1.9. Additionally, this is only used internally by the client. On click, opens a twitch user info GUI screen. Value should be the twitch user name.
    /// suggest_command
    ///		Only usable for messages in chat. Replaces the content of the chat box with the given text - usually a command, but it is not required to be a command (commands should be prefixed with /).
    /// change_page
    ///     Only usable within written books. Changes the page of the book to the given page, starting at 1. For instance, "value":1 switches the book to the first page. If the page is less than one or beyond the number of pages in the book, the event is ignored.
    /// </summary>
    public class ClickEventPayLoad
    {
        [JsonProperty(PropertyName = "action")]
        public string Action { get; set; }

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }
}
