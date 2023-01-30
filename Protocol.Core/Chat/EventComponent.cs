using System.Text.Json.Serialization;

namespace MinecraftProtocol.Chat
{
    public class EventComponent<T>
    {
        [JsonPropertyName("action")]
        public EventAction Action;

        [JsonPropertyName("value")]
        public T Value;

        [JsonPropertyName("contents")]
        public T Contents;

        public EventComponent() { }
        public EventComponent(EventAction action, T value)
        {
            Action = action;
            Value = value;
        }
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EventAction
    {
        /// <summary>
        /// Opens the given URL in the default web browser. Ignored if the player has opted to disable links in chat;
        /// may open a GUI prompting the user if the setting for that is enabled. The link's protocol must be set and must be http or https, for security reasons.
        /// 
        /// Only used for click event.
        /// </summary>
        open_url,
        /// <summary>
        /// Cannot be used within JSON chat. Opens a link to any protocol, but cannot be used in JSON chat for security reasons. Only exists to internally implement links for screenshots.
        /// 
        /// Only used for click event.
        /// </summary>
        open_file,
        /// <summary>
        /// Runs the given command. Not required to be a command - clicking this only causes the client to send the given content as a chat message, so if not prefixed with /, they will say the given text instead. 
        /// If used in a book GUI, the GUI is closed after clicking.
        /// 
        /// Only used for click event.
        /// </summary>
        run_command,
        /// <summary>
        /// No longer supported; cannot be used within JSON chat. Only usable in 1.8 and below;
        /// twitch support was removed in 1.9. Additionally, this is only used internally by the client.
        /// On click, opens a twitch user info GUI screen. Value should be the twitch user name.
        /// 
        /// Only used for click event.
        /// </summary>
        twitch_user_info,
        /// <summary>
        /// Only usable for messages in chat.
        /// Replaces the content of the chat box with the given text - usually a command, but it is not required to be a command (commands should be prefixed with /).
        /// 
        /// Only used for click event.
        /// </summary>
        suggest_command,
        /// <summary>
        /// Only usable within written books.
        /// Changes the page of the book to the given page, starting at 1. For instance, "value":1 switches the book to the first page.
        /// If the page is less than one or beyond the number of pages in the book, the event is ignored.
        /// 
        /// Only used for click event.
        /// </summary>
        change_page,



        /// <summary>
        /// The text to display.
        /// Can either be a string directly ("value":"la") or a full component ("value":{"text":"la","color":"red"}).
        /// 
        /// Only used for hover event.
        /// </summary>
        show_text,
        /// <summary>
        /// The NBT of the item to display, in the JSON-NBT format (as would be used in /give).
        /// Note that this is a String and not a JSON object - it should either be set in a String directly ("value":"{id:35,Damage:5,Count:2,tag:{display:{Name:Testing}}}") or as text of a component ("value":{"text":"{id:35,Damage:5,Count:2,tag:{display:{Name:Testing}}}"}). 
        /// If the item is invalid, "Invalid Item!" will be drawn in red instead.
        /// 
        /// Only used for hover event.
        /// </summary>
        show_item,
        /// <summary>
        /// A JSON-NBT String describing the entity. Contains 3 values: id, the entity's UUID (with dashes); type (optional), which contains the resource location for the entity's type (eg minecraft:zombie); and name, which contains the entity's custom name (if present).
        /// Note that this is a String and not a JSON object. It should be set in a String directly ("value":"{id:7e4a61cc-83fa-4441-a299-bf69786e610a,type:minecraft:zombie,name:Zombie}") or as the content of a component.  If the entity is invalid, "Invalid Entity!" will be displayed.
        /// Note that the client does not need to have the given entity loaded.
        /// 
        /// Only used for hover event.
        /// </summary>
        show_entity,
        /// <summary>
        /// No longer supported. Since 1.12, this no longer exists; advancements instead simply use show_text.
        /// The ID of an achievement or statistic to display. Example: "value":"achievement.openInventory".
        /// 
        /// Only used for hover event.
        /// </summary>
        show_achievement
    }

}