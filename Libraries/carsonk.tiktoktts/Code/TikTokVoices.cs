using Sandbox;

namespace TikTokTTS;

public enum TikTokVoice
{
    [Icon( "🇺🇸" ), Title( "US Female - Jessie (en_us_002)" )] en_us_002,
    [Icon( "🇺🇸" ), Title( "US Female - Empathetic (en_female_samc)" )] en_female_samc,
    [Icon( "🇺🇸" ), Title( "US Female - Beauty Guru (en_female_makeup)" )] en_female_makeup,
    [Icon( "🇺🇸" ), Title( "US Female - Bestie (en_female_richgirl)" )] en_female_richgirl,

    [Icon( "🇺🇸" ), Title( "US Male - Joey (en_us_006)" )] en_us_006,
    [Icon( "🇺🇸" ), Title( "US Male - Professor (en_us_007)" )] en_us_007,
    [Icon( "🇺🇸" ), Title( "US Male - Scientist (en_us_009)" )] en_us_009,
    [Icon( "🇺🇸" ), Title( "US Male - Confidence (en_us_010)" )] en_us_010,
    [Icon( "🇺🇸" ), Title( "US Male - Serious (en_male_cody)" )] en_male_cody,
    [Icon( "🇺🇸" ), Title( "US Male - Wacky (en_male_funny)" )] en_male_funny,
    [Icon( "🇺🇸" ), Title( "US Male - Story Teller (en_male_narration)" )] en_male_narration,
    [Icon( "🇺🇸" ), Title( "US Male - Game On (en_male_jomboy)" )] en_male_jomboy,
    [Icon( "🇺🇸" ), Title( "US Male - Alfred (en_male_jarvis)" )] en_male_jarvis,

    [Icon( "🇬🇧" ), Title( "UK Male - Narrator (en_uk_001)" )] en_uk_001,
    [Icon( "🇬🇧" ), Title( "UK Male - English (en_uk_003)" )] en_uk_003,
    [Icon( "🇬🇧" ), Title( "UK Male - Olantekkers (en_male_olantekkers)" )] en_male_olantekkers,

    [Icon( "🇦🇺" ), Title( "AU Female - Metro (en_au_001)" )] en_au_001,
    [Icon( "🇦🇺" ), Title( "AU Female - Peaceful (en_female_emotional)" )] en_female_emotional,
    [Icon( "🇦🇺" ), Title( "AU Male - Smooth (en_au_002)" )] en_au_002,
    [Icon( "🇦🇺" ), Title( "AU Male - Ashmagic (en_male_ashmagic)" )] en_male_ashmagic,

    [Icon( "💘" ), Title( "Festive - Cupid (en_male_cupid)" )] en_male_cupid,
    [Icon( "🎅" ), Title( "Festive - Narration (en_male_santa_narration)" )] en_male_santa_narration,
    [Icon( "🎅" ), Title( "Festive - Santa (en_male_santa_effect)" )] en_male_santa_effect,

    [Icon( "😱" ), Title( "Disney - Scream (en_us_ghostface)" )] en_us_ghostface,
    [Icon( "🐶" ), Title( "Disney - Chewbacca (en_us_chewbacca)" )] en_us_chewbacca,
    [Icon( "🤖" ), Title( "Disney - C3PO (en_us_c3po)" )] en_us_c3po,
    [Icon( "🥶" ), Title( "Disney - Stitch (en_us_stitch)" )] en_us_stitch,
    [Icon( "🪖" ), Title( "Disney - Stormtrooper (en_us_stormtrooper)" )] en_us_stormtrooper,
    [Icon( "🚀" ), Title( "Disney - Rocket (en_us_rocket)" )] en_us_rocket,
    [Icon( "👩‍🦳" ), Title( "Disney - Madame Leota (en_female_madam_leota)" )] en_female_madam_leota,
    [Icon( "👻" ), Title( "Disney - Ghost Host (en_male_ghosthost)" )] en_male_ghosthost,
    [Icon( "☠️" ), Title( "Disney - Pirate (en_male_pirate)" )] en_male_pirate,

    [Icon( "🧽" ), Title( "Silly - Trickster/Spongebob (en_male_grinch)" )] en_male_grinch,
    [Icon( "🧔" ), Title( "Silly - Mr. GoodGuy (en_male_deadpool)" )] en_male_deadpool,
    [Icon( "🙍" ), Title( "Silly - Lord Cringe (en_male_ukneighbor)" )] en_male_ukneighbor,
    [Icon( "🤵" ), Title( "Silly - Mr. Meticulous (en_male_ukbutler)" )] en_male_ukbutler,
    [Icon( "🥂" ), Title( "Silly - Debutante (en_female_shenna)" )] en_female_shenna,
    [Icon( "🤪" ), Title( "Silly - Varsity (en_female_pansino)" )] en_female_pansino,
    [Icon( "👨‍🔬" ), Title( "Silly - Marty (en_male_trevor)" )] en_male_trevor,
    [Icon( "🙅" ), Title( "Silly - Bae (en_female_betty)" )] en_female_betty,
    [Icon( "👵" ), Title( "Silly - Granny (en_female_grandma)" )] en_female_grandma,
    [Icon( "🪄" ), Title( "Silly - Magician (en_male_wizard)" )] en_male_wizard,

    [Icon( "🎶" ), Title( "Music - Alto/Cottagecore (en_female_f08_salut_damour)" )] en_female_f08_salut_damour,
    [Icon( "🎶" ), Title( "Music - Tenor/Jingle (en_male_m03_lobby)" )] en_male_m03_lobby,
    [Icon( "🎶" ), Title( "Music - Sunshine Soon/Toon Beat (en_male_m03_sunshine_soon)" )] en_male_m03_sunshine_soon,
    [Icon( "🎶" ), Title( "Music - Open Mic (en_female_f08_warmy_breeze)" )] en_female_f08_warmy_breeze,
    [Icon( "🎶" ), Title( "Music - Euphoric (en_female_ht_f08_glorious)" )] en_female_ht_f08_glorious,
    [Icon( "🎶" ), Title( "Music - It Goes Up/Hypetrain (en_male_sing_funny_it_goes_up)" )] en_male_sing_funny_it_goes_up,
    [Icon( "🎶" ), Title( "Music - Chipmunk/Quirky Time (en_male_m2_xhxs_m03_silly)" )] en_male_m2_xhxs_m03_silly,
    [Icon( "🎶" ), Title( "Music - Melodrama (en_female_ht_f08_wonderful_world)" )] en_female_ht_f08_wonderful_world,

    [Icon( "🎶" ), Title( "Parody - Pop Lullaby (en_female_f08_twinkle)" )] en_female_f08_twinkle,
    [Icon( "🎶" ), Title( "Parody - Classic Electric (en_male_m03_classical)" )] en_male_m03_classical,
    [Icon( "🎶" ), Title( "Parody - Cozy (en_male_m2_xhxs_m03_christmas)" )] en_male_m2_xhxs_m03_christmas,
    [Icon( "🎶" ), Title( "Parody - Caroler (en_male_sing_deep_jingle)" )] en_male_sing_deep_jingle,
    [Icon( "🎶" ), Title( "Parody - NYE 2023 (en_female_ht_f08_newyear)" )] en_female_ht_f08_newyear,
    [Icon( "🎶" ), Title( "Parody - Opera (en_female_ht_f08_halloween)" )] en_female_ht_f08_halloween,
    [Icon( "🎶" ), Title( "Parody - Thanksgiving (en_male_sing_funny_thanksgiving)" )] en_male_sing_funny_thanksgiving,

    [Icon( "🇫🇷" ), Title( "French - Male 1 (fr_001)" )] fr_001,
    [Icon( "🇫🇷" ), Title( "French - Male 2 (fr_002)" )] fr_002,

    [Icon( "🇪🇸" ), Title( "Spanish - Spain Male (es_002)" )] es_002,

    [Icon( "🇲🇽" ), Title( "Spanish - MX Male (es_mx_002)" )] es_mx_002,

    [Icon( "🇧🇷" ), Title( "Portuguese BR - Female 1 (br_001)" )] br_001,
    [Icon( "🇧🇷" ), Title( "Portuguese BR - Female 2 (br_003)" )] br_003,
    [Icon( "🇧🇷" ), Title( "Portuguese BR - Female 3 (br_004)" )] br_004,
    [Icon( "🇧🇷" ), Title( "Portuguese BR - Male (br_005)" )] br_005,

    [Icon( "🇩🇪" ), Title( "German - Female (de_001)" )] de_001,
    [Icon( "🇩🇪" ), Title( "German - Male (de_002)" )] de_002,

    [Icon( "🇮🇩" ), Title( "Indonesian - Female (id_001)" )] id_001,

    [Icon( "🇯🇵" ), Title( "Japanese - Female 1 (jp_001)" )] jp_001,
    [Icon( "🇯🇵" ), Title( "Japanese - Female 2 (jp_003)" )] jp_003,
    [Icon( "🇯🇵" ), Title( "Japanese - Female 3 (jp_005)" )] jp_005,
    [Icon( "🇯🇵" ), Title( "Japanese - Male (jp_006)" )] jp_006,

    [Icon( "🇰🇷" ), Title( "Korean - Female 1 (kr_003)" )] kr_003,
    [Icon( "🇰🇷" ), Title( "Korean - Male 1 (kr_002)" )] kr_002,
    [Icon( "🇰🇷" ), Title( "Korean - Male 2 (kr_004)" )] kr_004,

    [Icon( "🇻🇳" ), Title( "Vietnamese - Female (BV074_streaming)" )] BV074_streaming,
    [Icon( "🇻🇳" ), Title( "Vietnamese - Male (BV075_streaming)" )] BV075_streaming,



}