using System.Collections.Generic;

// The music behind the card game (MusicUI): note names (C D E F G A B / โด เร มี ฟา ซอล ลา ที), notes
// spelled properly with sharps and flats, the major / minor / seventh chords and the major and minor
// scales built on each note, the แคน / พิณ modes (ลาย: pentatonic), guitar chord shapes, and the cards
// themselves — dealt from a seed, a different hand for each instrument and harder as you level up.
public static class MusicTheory
{
    public static readonly string[] Letters = { "C", "D", "E", "F", "G", "A", "B" };
    public static readonly string[] Solfege = { "โด", "เร", "มี", "ฟา", "ซอล", "ลา", "ที" };
    private static readonly int[] NaturalPc = { 0, 2, 4, 5, 7, 9, 11 };
    // the black keys, named both ways
    public static readonly string[] SharpNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    public static readonly string[] FlatNames = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };

    // A written note: a letter in an octave (step: C4 = 28, D4 = 29 … one per line or space) and its accidental
    public struct Pitch
    {
        public int Step;
        public int Acc;   // -1 flat, 0 natural, +1 sharp
        public Pitch(int step, int acc = 0) { Step = step; Acc = acc; }
        public int Letter => ((Step % 7) + 7) % 7;
        public int Octave => Step >= 0 ? Step / 7 : (Step - 6) / 7;
        public int Midi => (Octave + 1) * 12 + NaturalPc[Letter] + Acc;
        public int Pc => ((Midi % 12) + 12) % 12;
        public string Name => Letters[Letter] + (Acc > 0 ? "#" : Acc < 0 ? "b" : "");
        public string Thai => Solfege[Letter] + (Acc > 0 ? "#" : Acc < 0 ? "b" : "");
    }

    public static int StepOf(int letter, int octave) => octave * 7 + letter;
    public const int TrebleBottom = 30; // E4: the treble staff's bottom line
    public const int BassBottom = 18;   // G2: the bass staff's bottom line

    // Notes stacked on a root by letter (a third is two letters up), each sharpened or flattened to
    // the semitones asked for: chords and scales come out spelled as on paper (F major has B flat)
    private static List<Pitch> Build(int rootLetter, int rootAcc, int octave, int[] steps, int[] semis)
    {
        var root = new Pitch(StepOf(rootLetter, octave), rootAcc);
        var list = new List<Pitch>();
        for (int i = 0; i < steps.Length; i++)
        {
            var p = new Pitch(root.Step + steps[i]);
            int want = root.Midi + semis[i], have = p.Midi;
            p.Acc = want - have;
            list.Add(p);
        }
        return list;
    }

    public enum ChordKind { Major, Minor, Seventh }

    public static List<Pitch> Chord(int rootLetter, ChordKind kind, int octave = 4) =>
        kind == ChordKind.Major ? Build(rootLetter, 0, octave, new[] { 0, 2, 4 }, new[] { 0, 4, 7 })
        : kind == ChordKind.Minor ? Build(rootLetter, 0, octave, new[] { 0, 2, 4 }, new[] { 0, 3, 7 })
        : Build(rootLetter, 0, octave, new[] { 0, 2, 4, 6 }, new[] { 0, 4, 7, 10 });

    public static string ChordName(int rootLetter, ChordKind kind) =>
        Letters[rootLetter] + (kind == ChordKind.Minor ? "m" : kind == ChordKind.Seventh ? "7" : "");

    public static string ChordThai(ChordKind kind) =>
        kind == ChordKind.Major ? "เมเจอร์" : kind == ChordKind.Minor ? "ไมเนอร์" : "โดมินันต์เซเว่น";

    // The major scale (and the natural minor) up an octave: eight notes
    public static List<Pitch> Scale(int rootLetter, bool minor, int octave = 4) =>
        Build(rootLetter, 0, octave, new[] { 0, 1, 2, 3, 4, 5, 6, 7 },
            minor ? new[] { 0, 2, 3, 5, 7, 8, 10, 12 } : new[] { 0, 2, 4, 5, 7, 9, 11, 12 });

    // ลายแคน / ลายพิณ: the Isan modes, each a five-note (pentatonic) scale
    public static readonly (string name, string about, int rootLetter, int[] letters)[] Lai =
    {
        ("ลายใหญ่", "เพนทาโทนิก A ไมเนอร์", 5, new[] { 5, 0, 1, 2, 4 }),       // A C D E G
        ("ลายน้อย", "เพนทาโทนิก D ไมเนอร์", 1, new[] { 1, 3, 4, 5, 0 }),       // D F G A C
        ("ลายโป้ซ้าย", "เพนทาโทนิก C เมเจอร์", 0, new[] { 0, 1, 2, 4, 5 }),    // C D E G A
        ("ลายสุดสะแนน", "เพนทาโทนิก G เมเจอร์", 4, new[] { 4, 5, 6, 1, 2 }),   // G A B D E
    };

    public static List<Pitch> LaiNotes(int index)
    {
        var (_, _, root, letters) = Lai[index];
        var list = new List<Pitch>();
        int baseStep = StepOf(root, 4);
        foreach (int l in letters)
        {
            int step = StepOf(l, 4);
            while (step < baseStep) step += 7;
            list.Add(new Pitch(step));
        }
        return list;
    }

    // Open-position guitar shapes, low E string to high e: x muted, 0 open, 1-4 the fret
    public static string GuitarShape(string chord)
    {
        switch (chord)
        {
            case "C": return "x32010";
            case "D": return "xx0232";
            case "E": return "022100";
            case "F": return "133211";
            case "G": return "320003";
            case "A": return "x02220";
            case "B": return "x24442";
            case "Cm": return "x35543";
            case "Dm": return "xx0231";
            case "Em": return "022000";
            case "Fm": return "133111";
            case "Gm": return "355333";
            case "Am": return "x02210";
            case "Bm": return "x24432";
            case "C7": return "x32310";
            case "D7": return "xx0212";
            case "E7": return "020100";
            case "F7": return "131211";
            case "G7": return "320001";
            case "A7": return "x02020";
            case "B7": return "x21202";
            default: return null;
        }
    }

    // ── Drums (percussion staff: where each one is written, and its notehead) ────────────
    public static readonly (string name, string thai, int pos, bool cross)[] Drums =
    {
        ("Kick", "กลองเบส", 1, false),    // bottom space
        ("Snare", "สแนร์", 5, false),     // third space
        ("Hi-hat", "ไฮแฮต", 9, true),     // above the top line, a cross
        ("Tom", "ทอม", 7, false),         // top space
        ("Crash", "แฉ", 10, true),        // on a ledger line above, a cross
    };

    // ── The cards ───────────────────────────────────────────────────────────────────────────
    public enum Kind { Symbol, Beats, Count, Read, Chord, Scale, Lai, Root, Drum, Groove }

    // ── Note values and rests (ค่าตัวโน้ต): the first lessons ───────────────────────────────
    // hollow: an open head; flags: เขบ็ด; a rest is drawn as its own sign
    public static readonly (string thai, float beats, bool rest, bool hollow, bool stem, int flags)[] Values =
    {
        ("ตัวกลม", 4f, false, true, false, 0),
        ("ตัวขาว", 2f, false, true, true, 0),
        ("ตัวดำ", 1f, false, false, true, 0),
        ("ตัวเขบ็ด 1 ชั้น", 0.5f, false, false, true, 1),
        ("ตัวเขบ็ด 2 ชั้น", 0.25f, false, false, true, 2),
        ("ตัวหยุดตัวกลม", 4f, true, false, false, 0),
        ("ตัวหยุดตัวขาว", 2f, true, false, false, 0),
        ("ตัวหยุดตัวดำ", 1f, true, false, false, 0),
        ("ตัวหยุดเขบ็ด 1 ชั้น", 0.5f, true, false, false, 1),
    };

    public static string BeatText(float beats)
    {
        int whole = (int)beats;
        float part = beats - whole;
        string frac = part >= 0.74f ? "3/4" : part >= 0.49f ? "1/2" : part >= 0.24f ? "1/4" : "";
        return whole == 0 ? (frac == "" ? "0" : frac) : frac == "" ? whole.ToString() : $"{whole} {frac}";
    }

    // ── The lessons (บทเรียน): what a musician of each level is dealt ────────────────────────
    public static readonly int[] LessonStarts = { 1, 3, 5, 8, 12, 18, 25 };

    public static string Lesson(string instrument, int level)
    {
        bool drums = instrument == "drums";
        if (level < 3) return "บทที่ 1 · รู้จักตัวโน้ต";
        if (level < 5) return "บทที่ 2 · ตัวหยุดและการนับจังหวะ";
        if (level < 8) return drums ? "บทที่ 3 · อ่านโน้ตกลอง" : "บทที่ 3 · อ่านโน้ตบนบรรทัด 5 เส้น";
        if (level < 12) return drums ? "บทที่ 4 · จังหวะพื้นฐาน" : "บทที่ 4 · บันไดเสียง";
        if (level < 18) return drums ? "บทที่ 5 · จังหวะกับกลองทอม"
            : instrument == "bass" ? "บทที่ 5 · โน้ตรากของคอร์ด" : IsFolk(instrument) ? "บทที่ 5 · ลายแคน ลายพิณ" : "บทที่ 5 · คอร์ด";
        if (level < 25) return drums ? "บทที่ 6 · จังหวะกับแฉ" : "บทที่ 6 · ไมเนอร์และเครื่องหมายแปลงเสียง";
        return "บทที่ 7 · มืออาชีพ";
    }

    // The level the next lesson opens at (0: this is the last)
    public static int NextLesson(int level)
    {
        foreach (int at in LessonStarts) if (at > level) return at;
        return 0;
    }

    // ── Where each lesson is taught (music.lua VENUES.cap) ──────────────────────────────────
    // The village sala is the practice room: the basics only (บทที่ 1-2). The harder lessons are on
    // the bigger stages: the temple fair บทที่ 3, the tambon stage 4, the town's stage and pubs 5, the
    // province's stage 6, and the national stages (สนามหลวง, the beach concerts) everything.
    // The cap is the highest level whose lesson is dealt there.
    public static int VenueCap(string venue)
    {
        switch (venue)
        {
            case "village_hall": return 4;
            case "temple_fair": return 7;
            case "tambon_event": return 11;
            case "city_event":
            case "pub_puea_chiwit":
            case "pub_blues":
            case "pub_indie": return 17;
            case "province_stage": return 24;
            default: return 100; // beach_concert, national_stage
        }
    }

    // The level the cards are dealt at on this stage
    public static int LessonLevel(string venue, int level) => System.Math.Min(level, VenueCap(venue));

    // Where to go for the lesson after this stage's last one
    public static string NextStage(string venue)
    {
        int cap = VenueCap(venue);
        return cap <= 4 ? "งานวัด / เวทีตำบล" : cap <= 7 ? "เวทีอีเวนต์ตำบล" : cap <= 11 ? "เวทีอีเวนต์เมือง หรือผับ"
            : cap <= 17 ? "เวทีดนตรีประจำจังหวัด" : cap <= 24 ? "เวทีระดับประเทศ (สนามหลวง กรุงเทพฯ / คอนเสิร์ตริมหาด)" : "";
    }

    public class Card
    {
        public Kind Kind;
        public string Badge;          // อ่านโน้ต, คอร์ด, บันไดเสียง ...
        public string Title;          // "คอร์ด G", "บันไดเสียง D เมเจอร์"
        public string Ask;            // what to do
        public string Reveal;         // the answer spelled out, shown after
        public bool Bass;             // bass clef
        public List<Pitch> Notes = new List<Pitch>(); // what the staff shows / fills in
        public int Hidden = -1;       // a scale card: the note left out
        public bool FillIn;           // the staff starts empty and fills as you find the notes
        public int[] Answer;          // pitch classes (keys) or drum indexes (pads)
        public bool Ordered;          // must be played in order (a drum groove)
        public string Shape;          // guitar chord diagram
        public int[] Symbols;         // note values / rests shown (indexes into Values)
        public string[] Choices;      // a multiple-choice card: buttons instead of keys (answers 100 + i)
        public float Seconds;         // the fuse
    }

    // Instruments as the server names them: pin khaen so ranat (folk), guitar drums bass keyboard
    public static bool IsFolk(string instrument) => instrument == "pin" || instrument == "khaen" || instrument == "so" || instrument == "ranat";

    public static List<Card> Deal(string instrument, int level, int count, int seed)
    {
        var rng = new System.Random(seed);
        var cards = new List<Card>();
        bool drums = instrument == "drums", bass = instrument == "bass";
        for (int i = 0; i < count; i++)
        {
            Card c;
            int roll = rng.Next(100);
            if (level < 3) // บทที่ 1: the notes — whole, half, quarter, eighth
                c = roll < 65 ? SymbolCard(rng, level) : BeatsCard(rng, level);
            else if (level < 5) // บทที่ 2: rests, and counting beats
                c = roll < 40 ? SymbolCard(rng, level) : roll < 75 ? BeatsCard(rng, level) : CountCard(rng, level);
            else if (level < 8) // บทที่ 3: reading the staff (drums: where each drum is written)
                c = roll < 25 ? (roll < 10 ? BeatsCard(rng, level) : CountCard(rng, level))
                    : drums ? DrumRead(rng) : ReadFor(rng, instrument, level);
            else if (level < 12) // บทที่ 4: the easy major scales (drums: simple grooves)
                c = roll < 15 ? CountCard(rng, level)
                    : roll < 45 ? (drums ? DrumRead(rng) : ReadFor(rng, instrument, level))
                    : drums ? Groove(rng, level) : ScaleCard(rng, level, bass);
            else // บทที่ 5 on: each instrument's own cards, minors and accidentals from บทที่ 6
            {
                if (roll < 10) c = CountCard(rng, level);
                else
                {
                    roll = rng.Next(100);
                    switch (instrument)
                    {
                        case "drums":
                            c = roll < 30 ? DrumRead(rng) : Groove(rng, level);
                            break;
                        case "bass":
                            c = roll < 30 ? Read(rng, level, bass: true) : roll < 70 ? Root(rng, level) : ScaleCard(rng, level, bass: true);
                            break;
                        case "guitar":
                        case "keyboard":
                            c = roll < 25 ? ReadFor(rng, instrument, level)
                                : roll < 70 ? ChordCard(rng, level, instrument == "guitar") : ScaleCard(rng, level, bass: false);
                            break;
                        default: // the folk instruments: read, the ลาย, and the plain scales
                            c = roll < 30 ? Read(rng, level, bass: false) : roll < 70 ? LaiCard(rng) : ScaleCard(rng, level, bass: false);
                            break;
                    }
                }
            }
            // the fuse: generous at first, quicker with experience, longer for each extra note to find
            float per = Lerp(8f, 3.6f, (level - 1) / 40f) + (c.Choices != null ? 1.5f : 0f);
            c.Seconds = per + (c.Answer.Length - 1) * Lerp(2.2f, 1.4f, (level - 1) / 40f);
            cards.Add(c);
        }
        return cards;
    }

    // The keyboard reads treble, and the bass clef too once it is past the basics
    private static Card ReadFor(System.Random rng, string instrument, int level) =>
        Read(rng, level, bass: instrument == "bass" || (instrument == "keyboard" && level >= 12 && rng.Next(3) == 0));

    // ── Rhythm cards: multiple choice ─────────────────────────────────────────────────────
    // Which signs each lesson knows: the notes first (no เขบ็ด 2 ชั้น yet), then the rests
    private static int[] Known(int level) =>
        level < 3 ? new[] { 0, 1, 2, 3 } : new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };

    // Four answers, the right one among them, shuffled; returns where the right one went
    private static int Shuffle(System.Random rng, List<string> options, string right)
    {
        for (int i = options.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (options[i], options[j]) = (options[j], options[i]);
        }
        return options.IndexOf(right);
    }

    private static List<string> Pick(System.Random rng, List<string> pool, string right, int n)
    {
        var rest = new List<string>();
        foreach (var o in pool) if (o != right && !rest.Contains(o)) rest.Add(o);
        var list = new List<string> { right };
        while (list.Count < n && rest.Count > 0)
        {
            int k = rng.Next(rest.Count);
            list.Add(rest[k]);
            rest.RemoveAt(k);
        }
        return list;
    }

    // What is this sign called?
    private static Card SymbolCard(System.Random rng, int level)
    {
        var known = Known(level);
        int v = known[rng.Next(known.Length)];
        var pool = new List<string>();
        foreach (int k in known) pool.Add(Values[k].thai);
        var options = Pick(rng, pool, Values[v].thai, 4);
        int at = Shuffle(rng, options, Values[v].thai);
        return new Card
        {
            Kind = Kind.Symbol, Badge = "สัญลักษณ์", Title = "สัญลักษณ์นี้ชื่ออะไร?",
            Ask = Values[v].rest ? "ตัวหยุด = เงียบ ไม่เล่นเสียง ตามจำนวนจังหวะ" : "ดูหัวโน้ต (โปร่ง/ทึบ) หาง และเขบ็ด",
            Symbols = new[] { v }, Choices = options.ToArray(), Answer = new[] { 100 + at },
            Reveal = $"{Values[v].thai} = {BeatText(Values[v].beats)} จังหวะ",
        };
    }

    // How many beats is this sign worth?
    private static Card BeatsCard(System.Random rng, int level)
    {
        var known = Known(level);
        int v = known[rng.Next(known.Length)];
        string right = BeatText(Values[v].beats);
        var pool = new List<string> { "4", "2", "1", "1/2" };
        if (level >= 3) pool.Add("1/4");
        var options = Pick(rng, pool, right, 4);
        int at = Shuffle(rng, options, right);
        return new Card
        {
            Kind = Kind.Beats, Badge = "ค่าจังหวะ", Title = "สัญลักษณ์นี้ยาวกี่จังหวะ?",
            Ask = "ตัวกลม 4 · ตัวขาว 2 · ตัวดำ 1 · เขบ็ด 1 ชั้น ครึ่งจังหวะ",
            Symbols = new[] { v }, Choices = options.ToArray(), Answer = new[] { 100 + at },
            Reveal = $"{Values[v].thai} = {right} จังหวะ",
        };
    }

    // Two or three signs in a row: how many beats altogether?
    private static Card CountCard(System.Random rng, int level)
    {
        var known = Known(level);
        int n = level >= 8 ? 3 + rng.Next(2) : level >= 5 ? 3 : 2;
        var symbols = new int[n];
        float sum = 0f;
        var names = new List<string>();
        for (int i = 0; i < n; i++)
        {
            int v = known[rng.Next(known.Length)];
            if (sum + Values[v].beats > 6f) v = 3; // keep it short: an eighth instead
            symbols[i] = v;
            sum += Values[v].beats;
            names.Add(BeatText(Values[v].beats));
        }
        string right = BeatText(sum);
        var pool = new List<string>();
        foreach (float d in new[] { -2f, -1f, -0.5f, 0.5f, 1f, 2f })
            if (sum + d > 0f) pool.Add(BeatText(sum + d));
        var options = Pick(rng, pool, right, 4);
        int at = Shuffle(rng, options, right);
        return new Card
        {
            Kind = Kind.Count, Badge = "นับจังหวะ", Title = "รวมกันกี่จังหวะ?",
            Ask = "บวกค่าจังหวะของทุกตัว (ตัวหยุดก็นับ)",
            Symbols = symbols, Choices = options.ToArray(), Answer = new[] { 100 + at },
            Reveal = $"{string.Join(" + ", names)} = {right} จังหวะ",
        };
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * (t < 0f ? 0f : t > 1f ? 1f : t);

    // One note on the staff: what is it called?
    private static Card Read(System.Random rng, int level, bool bass)
    {
        int bottom = bass ? BassBottom : TrebleBottom;
        int lo = level >= 12 ? -2 : 0, hi = level >= 12 ? 10 : 8; // up and down to the ledger lines later
        var p = new Pitch(bottom + rng.Next(lo, hi + 1));
        if (level >= 18 && rng.Next(10) < 3) p.Acc = rng.Next(2) == 0 ? 1 : -1;
        return new Card
        {
            Kind = Kind.Read, Badge = "อ่านโน้ต", Title = "โน้ตตัวนี้คืออะไร?",
            Ask = "กดคีย์ให้ตรงกับโน้ตบนบรรทัด 5 เส้น", Bass = bass, Notes = { p }, Answer = new[] { p.Pc },
            Reveal = $"{p.Name}  ({p.Thai})",
        };
    }

    private static Card ChordCard(System.Random rng, int level, bool guitar)
    {
        var kind = ChordKind.Major;
        int r = rng.Next(100);
        if (level >= 18 && r < 35) kind = ChordKind.Minor;
        if (level >= 25 && r >= 80) kind = ChordKind.Seventh;
        int root = rng.Next(7);
        var notes = Chord(root, kind);
        string name = ChordName(root, kind);
        return new Card
        {
            Kind = Kind.Chord, Badge = "คอร์ด", Title = $"คอร์ด {name}",
            Ask = $"{ChordThai(kind)} · กดโน้ตทั้ง {notes.Count} ตัวในคอร์ด (ลำดับไหนก็ได้)",
            Notes = notes, FillIn = true, Answer = Pcs(notes),
            Reveal = $"คอร์ด {name} = {Names(notes)}", Shape = guitar ? GuitarShape(name) : null,
        };
    }

    private static Card ScaleCard(System.Random rng, int level, bool bass)
    {
        bool minor = level >= 18 && rng.Next(3) == 0;
        int root = minor ? new[] { 5, 1, 2 }[rng.Next(3)] // A D E minor
            : level < 10 ? new[] { 0, 4, 3 }[rng.Next(3)]   // C G F: none or one sharp/flat to start with
            : rng.Next(7);
        var notes = Scale(root, minor, bass ? (root >= 4 ? 2 : 3) : 4);
        int hidden = 1 + rng.Next(6); // never the root: you need it to know the key
        string key = Letters[root] + (minor ? " ไมเนอร์" : " เมเจอร์");
        return new Card
        {
            Kind = Kind.Scale, Badge = "บันไดเสียง", Title = $"บันไดเสียง {key}",
            Ask = "โน้ตที่หายไปคือตัวไหน?", Bass = bass, Notes = notes, Hidden = hidden,
            Answer = new[] { notes[hidden].Pc }, Reveal = $"{key}: {Names(notes)}",
        };
    }

    private static Card LaiCard(System.Random rng)
    {
        int i = rng.Next(Lai.Length);
        var notes = LaiNotes(i);
        return new Card
        {
            Kind = Kind.Lai, Badge = "ลายอีสาน", Title = Lai[i].name,
            Ask = $"{Lai[i].about} · กดโน้ตทั้ง 5 ตัวของลาย", Notes = notes, FillIn = true, Answer = Pcs(notes),
            Reveal = $"{Lai[i].name} = {ThaiNames(notes)}",
        };
    }

    // The bass player's job: the chord's root
    private static Card Root(System.Random rng, int level)
    {
        int root = rng.Next(7);
        var kind = level >= 18 && rng.Next(3) == 0 ? ChordKind.Minor : ChordKind.Major;
        string name = ChordName(root, kind);
        var p = new Pitch(StepOf(root, 2) + (root < 4 ? 7 : 0)); // the staff's middle
        return new Card
        {
            Kind = Kind.Root, Badge = "โน้ตราก", Title = $"คอร์ด {name}",
            Ask = "มือเบสเล่นโน้ตรากของคอร์ด: กดโน้ตราก", Bass = true, Notes = { p }, FillIn = true,
            Answer = new[] { p.Pc }, Reveal = $"รากของคอร์ด {name} คือ {p.Name}",
        };
    }

    private static Card DrumRead(System.Random rng)
    {
        int d = rng.Next(Drums.Length);
        return new Card
        {
            Kind = Kind.Drum, Badge = "โน้ตกลอง", Title = "เสียงนี้ตีอะไร?",
            Ask = "ดูตำแหน่งบนบรรทัดกับหัวโน้ต แล้วตีให้ถูก", Answer = new[] { d }, Reveal = $"{Drums[d].name} ({Drums[d].thai})",
        };
    }

    // A bar of four: hit them in order
    private static Card Groove(System.Random rng, int level)
    {
        var seq = new int[4];
        for (int i = 0; i < 4; i++) seq[i] = i % 2 == 0 ? (rng.Next(3) == 0 ? 2 : 0) : (level >= 12 && rng.Next(4) == 0 ? 3 : 1);
        if (level >= 18 && rng.Next(3) == 0) seq[0] = 4;
        var names = new List<string>();
        foreach (int d in seq) names.Add(Drums[d].thai);
        return new Card
        {
            Kind = Kind.Groove, Badge = "จังหวะ", Title = "ตีตามโน้ต 1 ห้อง", Ask = "ตีทีละตัวจากซ้ายไปขวา",
            Answer = seq, Ordered = true, Reveal = string.Join(" · ", names),
        };
    }

    private static int[] Pcs(List<Pitch> notes)
    {
        var set = new List<int>();
        foreach (var p in notes) if (!set.Contains(p.Pc)) set.Add(p.Pc);
        return set.ToArray();
    }

    private static string Names(List<Pitch> notes)
    {
        var s = new List<string>();
        foreach (var p in notes) s.Add(p.Name);
        return string.Join(" · ", s);
    }

    private static string ThaiNames(List<Pitch> notes)
    {
        var s = new List<string>();
        foreach (var p in notes) s.Add($"{p.Thai} ({p.Name})");
        return string.Join(" ", s);
    }
}
