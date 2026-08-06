// Writes a Dynamo example graph for every node, from the spec beside it.
//
// Why a generator rather than 114 hand-built graphs: they are the same graph with a different node
// in the middle, and a hand-built set drifts one file at a time. The spec is the thing to edit.
//
// It writes the whole .dyn including the View section — node positions, groups and the run mode.
// That part matters: saving a workspace over the Dynamo MCP drops View entirely, so a graph saved
// that way opens as an unpositioned pile with its groups gone.
//
//   node scripts/make-node-examples.mjs [--only Interlude.Input.TextBox]
//
// Verifying the result is a separate step, and is done in Dynamo: open each graph, check it reports
// no warnings, read the form back off its Form.ToJson node, and export the canvas picture.

import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const outputFolder = join(root, 'docs', 'nodes');

// Dynamo identifies everything by GUID, and a regenerated file that differs only in its GUIDs is a
// diff nobody can read. These are derived from the node name instead, so regenerating is a no-op.
let counter = 0;
const idFor = (seed) => {
    let h1 = 0x811c9dc5, h2 = 0x01000193;
    const text = seed + '|' + counter++;

    for (const character of text) {
        h1 = Math.imul(h1 ^ character.charCodeAt(0), 16777619) >>> 0;
        h2 = Math.imul(h2 + character.charCodeAt(0), 2654435761) >>> 0;
    }

    // >>> 0 is load-bearing: Math.imul returns a signed int, and a negative one renders as
    // "-1a2b3c4d". Dynamo takes GUIDs as 32 hex characters and silently declines to open a file
    // whose ids are not, leaving the previous graph on screen and reporting nothing.
    const chunk = (n, salt) => (Math.imul(n ^ salt, 2246822519) >>> 0)
        .toString(16).padStart(8, '0').slice(0, 8);
    return (chunk(h1, 1) + chunk(h2, 2) + chunk(h1 ^ h2, 3) + chunk(h1 + h2, 4)).slice(0, 32);
};

const port = (name, description, usesDefault) => ({
    Id: idFor('port:' + name + description),
    Name: name,
    Description: description ?? '',
    UsingDefaultValue: !!usesDefault,
    Level: 2,
    UseLevels: false,
    KeepListStructure: false,
});

/** A ZeroTouch node. `ports` are the input port names in order; `arity` how many are wired. */
const zeroTouch = (signature, label, inputs, outputs) => ({
    ConcreteType: 'Dynamo.Graph.Nodes.ZeroTouch.DSFunction, DynamoCore',
    Id: idFor('node:' + signature + label),
    NodeType: 'FunctionNode',
    Inputs: inputs.map((p, i) => port(p, '', i >= 0)),
    Outputs: outputs.map((p) => port(p, '')),
    FunctionSignature: signature,
    Replication: 'Auto',
    Description: '',
});

const codeBlock = (code, outputNames) => ({
    ConcreteType: 'Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore',
    Id: idFor('cb:' + code),
    NodeType: 'CodeBlockNode',
    Inputs: [],
    Outputs: outputNames.map((n) => port(n, '')),
    Replication: 'Disabled',
    Description: 'Allows for DesignScript code to be authored directly',
    Code: code,
});

const codeBlockWithInputs = (code, inputNames, outputNames) => {
    const node = codeBlock(code, outputNames);
    node.Inputs = inputNames.map((n) => port(n, ''));
    return node;
};

const stringNode = (value) => ({
    ConcreteType: 'CoreNodeModels.Input.StringInput, CoreNodeModels',
    SerializedWidth: 0,
    SerializedHeight: 0,
    Id: idFor('str:' + value),
    NodeType: 'StringInputNode',
    Inputs: [],
    Outputs: [port('', 'String')],
    Replication: 'Disabled',
    Description: 'Creates a string.',
    InputValue: value,
});

const boolNode = (value) => ({
    ConcreteType: 'CoreNodeModels.Input.BoolSelector, CoreNodeModels',
    Id: idFor('bool:' + value),
    NodeType: 'BooleanInputNode',
    Inputs: [],
    Outputs: [port('', 'Boolean')],
    Replication: 'Disabled',
    Description: 'Enables selection between True and False',
    InputValue: value,
});

const connect = (from, fromPort, to, toPort) => ({
    Start: from.Outputs[fromPort].Id,
    End: to.Inputs[toPort].Id,
    Id: idFor('conn:' + from.Id + fromPort + to.Id + toPort),
    IsHidden: 'False',
});

const view = (node, name, x, y) => ({
    Id: node.Id,
    Name: name,
    IsSetAsInput: false,
    IsSetAsOutput: false,
    Excluded: false,
    ShowGeometry: true,
    X: x,
    Y: y,
});

const group = (title, description, nodes, box, colour, styleId) => ({
    Id: idFor('group:' + title + nodes.map((n) => n.Id).join()),
    Title: title,
    DescriptionText: description,
    IsExpanded: true,
    WidthAdjustment: 0,
    HeightAdjustment: 0,
    UserSetWidth: 0,
    UserSetHeight: 0,
    Nodes: nodes.map((n) => n.Id),
    HasNestedGroups: false,
    Left: box.left,
    Top: box.top,
    Width: box.width,
    Height: box.height,
    FontSize: 36,
    GroupStyleId: styleId,
    InitialTop: box.top + 73,
    InitialHeight: box.height - 43,
    TextblockHeight: 63,
    IsOptionalInPortsCollapsed: false,
    IsUnconnectedOutPortsCollapsed: false,
    HasToggledOptionalInPorts: false,
    HasToggledUnconnectedOutPorts: false,
    Background: colour,
});

const STYLE_INPUTS = '883066aa-1fe2-44a4-9bd1-c3df86bfe9f6';
const STYLE_ACTIONS = '4d68be4a-a04d-4945-9dd5-cdf61079d790';
const STYLE_OUTPUTS = '07655dc1-2d65-4fed-8d6a-37235d3e3a8d';

/** The parameter types out of a Dynamo signature: everything after the @, comma-separated. */
const parameterTypes = (signature) => signature.split('@')[1]?.split(',') ?? [];

/**
 * A spec value as DesignScript source.
 *
 * The declared type decides the formatting, not the JSON: 0 written into a double port has to
 * come out as "0.0", because DesignScript reads a bare 0 as an integer and the node then binds to
 * a different overload than the one the signature names.
 */
function literal(value, type) {
    if (type === 'double') {
        return Number.isInteger(value) ? value.toFixed(1) : String(value);
    }

    if (type === 'int') {
        return String(Math.trunc(value));
    }

    if (type === 'bool') {
        return value ? 'true' : 'false';
    }

    // Strings, lists and anything typed var. JSON's syntax for both is DesignScript's.
    return JSON.stringify(value);
}

/** What Dynamo labels a code block's output port for a value of this type. */
const portType = (type) => ({
    string: 'string',
    double: 'double',
    int: 'integer',
    bool: 'boolean',
}[type] ?? 'var');

/**
 * A group box around everything upstream.
 *
 * Sized from where the nodes actually landed rather than from a guess, because how far the branch
 * reaches depends on how deeply the spec nests — one level for a Section, two for a Tabs.
 */
function branchBox(placements) {
    const left = Math.min(...placements.map((p) => p.x - 430)) - 30;
    const right = Math.max(...placements.map((p) => p.x)) + 360;
    const top = Math.min(...placements.map((p) => p.y)) - 190;
    const bottom = Math.max(...placements.map((p) => p.y)) + 430;

    return { left, top, width: right - left, height: bottom - top };
}

/**
 * The shape every element-producing sample shares: literals feed the node, the node becomes a
 * one-item list, the list is shown, and one answer is read back out by key.
 */
function buildElementGraph(spec) {
    // Everything upstream of the node the page is about, built depth first.
    //
    // A branch can be more than one node deep: Layout.Tabs holds TabPages, and a TabPage holds the
    // fields. So this recurses, and each level of nesting becomes another column further left.
    function buildBranch(branchSpec, depth, placements) {
        const branchTypes = parameterTypes(branchSpec.signature);
        const kids = (branchSpec.children ?? []).map((kid) => buildBranch(kid, depth + 1, placements));

        // The kids gathered into one list, for a port that takes several. Only when a port asks
        // for them all: a node that names its children one at a time would otherwise be left with
        // an unwired code block sitting next to it.
        const kidList = branchSpec.args.includes('$children')
            ? codeBlockWithInputs(
                '[' + kids.map((_, i) => 'child' + i).join(',') + '];',
                kids.map((_, i) => 'child' + i),
                ['list'])
            : null;

        // An argument is either a literal or a reference to what was just built. The literals
        // share one code block, so remember which port each of its outputs is heading for.
        const literals = [];
        const references = [];
        const extraLists = [];

        branchSpec.args.forEach((value, portIndex) => {
            if (value === '$children') {
                references.push({ port: portIndex, node: kidList });
            }
            else if (typeof value === 'string' && /^\$child\d+$/.test(value)) {
                references.push({ port: portIndex, node: kids[Number(value.slice(6))].node });
            }
            // A chosen few of the children, as a list — a port that takes several of something
            // when the node also takes other children that do not belong in it. Behavior nodes
            // are the case: one child is the field, the rest are the rules applied to it.
            else if (Array.isArray(value) && value.length > 0
                && value.every((v) => typeof v === 'string' && /^\$child\d+$/.test(v))) {
                const picked = value.map((v) => kids[Number(v.slice(6))].node);

                const listNode = codeBlockWithInputs(
                    '[' + picked.map((_, i) => 'item' + i).join(',') + '];',
                    picked.map((_, i) => 'item' + i),
                    ['list']);

                extraLists.push({ node: listNode, sources: picked });
                references.push({ port: portIndex, node: listNode });
            }
            else {
                literals.push({
                    port: portIndex,
                    code: literal(value, branchTypes[portIndex]),
                    type: portType(branchTypes[portIndex]),
                });
            }
        });

        // A node with nothing wired to it needs no code block. Theme.System is the case: it takes
        // no arguments at all, and an empty code block beside it would be a node that does nothing
        // and says nothing, which is worse than the gap it fills.
        const argsNode = literals.length > 0
            ? codeBlock(literals.map((l) => l.code + ';').join('\n'), literals.map((l) => l.type))
            : null;

        const node = zeroTouch(
            branchSpec.signature,
            branchSpec.name ?? branchSpec.node,
            branchSpec.ports,
            ['element']);

        placements.push({
            node,
            argsNode,
            kidList,
            extraLists,
            depth,
            name: branchSpec.name ?? branchSpec.node.split('.').slice(1).join('.'),
        });

        return {
            node,
            nodes: [
                ...kids.flatMap((k) => k.nodes),
                ...(kidList ? [kidList] : []),
                ...extraLists.map((l) => l.node),
                ...(argsNode ? [argsNode] : []),
                node,
            ],
            connectors: [
                ...kids.flatMap((k) => k.connectors),
                ...(kidList ? kids.map((k, i) => connect(k.node, 0, kidList, i)) : []),
                ...extraLists.flatMap((l) =>
                    l.sources.map((source, i) => connect(source, 0, l.node, i))),
                ...literals.map((l, i) => connect(argsNode, i, node, l.port)),
                ...references.map((r) => connect(r.node, 0, node, r.port)),
            ],
        };
    }

    // Most nodes return a form element, so the node the page is about is also the root of what
    // goes into the form. The rest — a condition, a rule, a computed value — are ingredients: they
    // mean nothing until something consumes them. Those specs carry a `graph`, whose root is
    // whatever does the consuming and which holds the page's node somewhere inside it.
    const placements = [];
    const branch = buildBranch(spec.graph ?? spec, 0, placements);
    const subject = branch.node;

    // One column per level of nesting, each stacked vertically and centred on the row above.
    const depthPitch = 1450;
    const rowPitch = 340;

    function layout(group, yBase) {
        const rows = new Map();

        for (const placement of group) {
            if (!rows.has(placement.depth)) {
                rows.set(placement.depth, []);
            }

            rows.get(placement.depth).push(placement);
        }

        for (const [, level] of rows) {
            level.forEach((placement, i) => {
                placement.x = 430 - placement.depth * depthPitch;
                placement.y = yBase + (i - (level.length - 1) / 2) * rowPitch;
            });
        }

        return Math.max(...group.map((p) => p.y));
    }

    let floor = layout(placements, 0);

    // A theme and an options bundle are not part of what the form contains; they are how it is
    // presented. Both are built exactly like the fields are and wired to the port of the same
    // name, and both are laid out underneath rather than in the same column, because a reader
    // looking for the form's contents should not have to step over its styling to find them.
    const sides = [];

    for (const side of ['theme', 'options']) {
        if (!spec[side]) {
            continue;
        }

        const sidePlacements = [];
        const built = buildBranch(spec[side], 0, sidePlacements);

        floor = layout(sidePlacements, floor + rowPitch * 2.5);
        sides.push({ side, built, placements: sidePlacements });
    }

    const themeSide = sides.find((s) => s.side === 'theme');
    const optionsSide = sides.find((s) => s.side === 'options');

    const list = codeBlockWithInputs('[field];', ['field'], ['list']);
    const title = stringNode(spec.title);
    const trigger = boolNode(false);
    const key = stringNode(spec.key);

    const show = zeroTouch(
        'Interlude.Form.Show@string,var[],var,string,string,double,double,string,bool,bool,var,var',
        'Form.Show',
        ['title', 'elements', 'trigger', 'submitText', 'cancelText', 'width', 'maxHeight',
            'formId', 'rememberValues', 'headlessUseDefaults', 'theme', 'options'],
        ['values', 'wasSubmitted', 'buttonClicked', 'form']);

    // A separator or a heading has no answer to read back, so those graphs stop at the form.
    const reader = spec.getter
        ? zeroTouch(spec.getter, spec.getterName, spec.getterPorts, ['value'])
        : null;

    const create = zeroTouch(
        'Interlude.Form.Create@string,var[],string,string,double,double,string,bool,bool,var,var',
        'Form.Create',
        ['title', 'elements', 'submitText', 'cancelText', 'width', 'maxHeight', 'formId',
            'rememberValues', 'headlessUseDefaults', 'theme', 'options'],
        ['form']);

    const toJson = zeroTouch(
        'Interlude.Form.ToJson@Interlude.Model.FormDefinition,bool',
        'Form.ToJson',
        ['form', 'indented'],
        ['json']);

    // Most readers take the result and a key. Some — Result.Keys, Result.WasSubmitted — take only
    // the result, and wiring a key string into a port they do not have would leave it dangling.
    const readsKey = reader ? (spec.getterPorts ?? []).includes('key') : false;

    // The round trip, for the Form nodes that treat a form as a file rather than as a dialog:
    // write it out, read it back, fill in the one field only the model knows, check what came of
    // it, and show that. None of the four means much on its own, so they share one graph and each
    // of their pages points at it.
    const roundTrip = spec.tail === 'document' ? (() => {
        const fromJson = zeroTouch('Interlude.Form.FromJson@string', 'Form.FromJson',
            ['json'], ['form']);

        const optionArgs = codeBlock(
            [JSON.stringify(spec.fillKey), JSON.stringify(spec.fillItems)]
                .map((c) => c + ';').join('\n'),
            ['string', 'var']);

        const withOptions = zeroTouch(
            'Interlude.Form.WithOptions@Interlude.Model.FormDefinition,string,var[],var[]',
            'Form.WithOptions', ['form', 'key', 'items', 'displayNames'], ['form']);

        const check = zeroTouch('Interlude.Form.Check@Interlude.Model.FormDefinition',
            'Form.Check', ['form'], ['isValid', 'messages']);

        const showAgain = zeroTouch('Interlude.Form.ShowDefinition@Interlude.Model.FormDefinition,var',
            'Form.ShowDefinition', ['form', 'trigger'], ['values', 'wasSubmitted', 'buttonClicked', 'form']);

        return {
            nodes: [fromJson, optionArgs, withOptions, check, showAgain],
            connectors: [
                connect(toJson, 0, fromJson, 0),
                connect(fromJson, 0, withOptions, 0),
                connect(optionArgs, 0, withOptions, 1),
                connect(optionArgs, 1, withOptions, 2),
                connect(withOptions, 0, check, 0),
                connect(withOptions, 0, showAgain, 0),
                connect(trigger, 0, showAgain, 1),
            ],
            views: [
                view(fromJson, 'Form.FromJson', 2840, 40),
                view(optionArgs, 'Code Block', 2840, 400),
                view(withOptions, 'Form.WithOptions', 3260, 40),
                view(check, 'Form.Check', 3700, 420),
                view(showAgain, 'Form.ShowDefinition', 3700, -100),
            ],
            box: { left: 2810, top: -230, width: 1330, height: 1030 },
        };
    })() : null;

    // Form.Forget clears the remembered answers for one form, so it stands beside the graph
    // rather than in it: nothing downstream consumes what it returns.
    const forget = spec.tail === 'forget' ? (() => {
        const formId = stringNode(spec.formId);
        const node = zeroTouch('Interlude.Form.Forget@string', 'Form.Forget',
            ['formId'], ['cleared']);

        // The same id goes to the form itself. A Forget wired to an id nothing uses would run
        // green and clear nothing, which is the one way this node can look like it works.
        return {
            nodes: [formId, node],
            connectors: [
                connect(formId, 0, node, 0),
                connect(formId, 0, show, 7),
                connect(formId, 0, create, 6),
            ],
            views: [view(formId, 'String', 2840, 40), view(node, 'Form.Forget', 3130, 40)],
            box: { left: 2810, top: -230, width: 720, height: 620 },
        };
    })() : null;

    const extra = roundTrip ?? forget;

    const nodes = [
        ...branch.nodes,
        ...sides.flatMap((s) => s.built.nodes),
        list, title, trigger, show, create, toJson,
        ...(reader ? [reader] : []),
        ...(readsKey ? [key] : []),
        ...(extra ? extra.nodes : []),
    ];

    const connectors = [
        ...branch.connectors,
        ...sides.flatMap((s) => s.built.connectors),
        connect(subject, 0, list, 0),
        connect(title, 0, show, 0),
        connect(list, 0, show, 1),
        connect(trigger, 0, show, 2),
        ...(themeSide ? [connect(themeSide.built.node, 0, show, 10)] : []),
        ...(optionsSide ? [connect(optionsSide.built.node, 0, show, 11)] : []),
        ...(reader ? [connect(show, 3, reader, 0)] : []),
        ...(readsKey ? [connect(key, 0, reader, 1)] : []),
        connect(title, 0, create, 0),
        connect(list, 0, create, 1),
        ...(themeSide ? [connect(themeSide.built.node, 0, create, 9)] : []),
        ...(optionsSide ? [connect(optionsSide.built.node, 0, create, 10)] : []),
        connect(create, 0, toJson, 0),
        ...(extra ? extra.connectors : []),
    ];

    // Upstream reads left to right: literals, the elements they make, the node under discussion.
    const viewsFor = (group) => group.flatMap((p) => [
        ...(p.argsNode ? [view(p.argsNode, 'Code Block', p.x - 430, p.y)] : []),
        view(p.node, p.name, p.x, p.y),
        ...(p.kidList ? [view(p.kidList, 'Code Block', p.x - 800, p.y)] : []),
        ...p.extraLists.map((l, j) => view(l.node, 'Code Block', p.x - 800, p.y + 200 + j * 180)),
    ]);

    const views = [
        ...viewsFor(placements),
        ...sides.flatMap((s) => viewsFor(s.placements)),
        view(title, 'String', 790, -190),
        view(list, 'Code Block', 790, 30),
        view(trigger, 'Boolean', 790, 190),
        ...(readsKey ? [view(key, 'String', 790, 400)] : []),
        view(show, 'Form.Show', 1080, 0),
        ...(reader ? [view(reader, spec.getterName, 1500, 460)] : []),
        view(create, 'Form.Create', 1990, 40),
        view(toJson, 'Form.ToJson', 2420, 40),
        ...(extra ? extra.views : []),
    ];

    // The note explains the node the page is about, so it belongs to whichever group actually
    // holds that node. On most pages that is the fields; on a Theme page it is the theme, and on
    // Form.FromJson's it is the round trip. A note filed under the wrong heading is worse than a
    // heading with no note, because the reader takes it as describing what it sits above.
    const subjectGroup =
        themeSide ? 'theme'
            : optionsSide ? 'options'
                : extra ? 'tail'
                    // A Result node IS the reader the template already ends in, and Form.Create
                    // and Form.ToJson are already the document tail. Neither needs a variation —
                    // only their note needs to land where the reader will be looking.
                    : spec.node.startsWith('Interlude.Result.') ? 'reader'
                        : /^Interlude\.Form\.(Create|ToJson)$/.test(spec.node) ? 'document'
                            : 'fields';
    const noteOn = (place, otherwise) => subjectGroup === place ? spec.note : otherwise;

    const groups = [
        group('Describe the field',
            noteOn('fields',
                'What the form holds, built left to right: literals into a code block, the code block into a node, and the nodes gathered into the list Form.Show takes.'),
            branch.nodes,
            branchBox(placements),
            '#FFB8D8', STYLE_INPUTS),
        ...sides.map((s) => group(
            s.side === 'theme' ? 'Dress it' : 'Set the extras',
            noteOn(s.side, s.side === 'theme'
                ? 'A theme is how the form looks rather than what it holds, so it is wired straight to Form.Show. The same theme goes to Form.Create, so the document keeps the appearance it was designed with.'
                : 'Form.Options carries the settings wanted rarely enough not to deserve a port on Form.Show of their own.'),
            s.built.nodes,
            branchBox(s.placements),
            '#FFC49B', STYLE_INPUTS)),
        group('Show it',
            'trigger is false so the graph runs without opening the dialog: every field comes back with its default and buttonClicked reads "skipped". Set it to true to see the form.',
            readsKey ? [title, list, trigger, key, show] : [title, list, trigger, show],
            { left: 760, top: -350, width: 700, height: 1180 },
            '#B9F6CA', STYLE_ACTIONS),
        ...(reader ? [group('Read the answer',
            noteOn('reader',
                'Answers come back keyed by the field\'s key, whatever the form looked like.'),
            [reader],
            { left: 1470, top: 300, width: 420, height: 380 },
            '#C1D676', STYLE_OUTPUTS)] : []),
        group('Keep it as a document',
            noteOn('document',
                'Form.Create builds the same form without showing it, and Form.ToJson writes it out. A form saved this way can be reviewed, diffed and loaded again with Form.FromJson.'),
            [create, toJson],
            { left: 1960, top: -120, width: 880, height: 800 },
            '#D8D8D8', STYLE_OUTPUTS),
        ...(roundTrip ? [group('Load it back',
            noteOn('tail',
                'The other half of the round trip. A checked-in form cannot carry model elements, so the file holds the layout, the labels and the validation, and the graph fills in the one field only the model knows before showing it.'),
            roundTrip.nodes,
            roundTrip.box,
            '#C1D676', STYLE_OUTPUTS)] : []),
        ...(forget ? [group('Forget the answers',
            noteOn('tail',
                'Answers are remembered per form between runs, which is what makes a form worth reopening. Form.Forget is how a graph starts again from the defaults.'),
            forget.nodes,
            forget.box,
            '#C1D676', STYLE_OUTPUTS)] : []),
    ];

    return { nodes, connectors, views, groups };
}

/**
 * The version a graph declares it needs, which is the version of the *package*.
 *
 * Not `versions.json`'s assemblyVersion: that is frozen at 1.0.0.0 on purpose, so that a graph
 * built against one release keeps resolving its nodes against the next. The number that moves is
 * VersionPrefix, and it is the one Dynamo matches against the installed package.
 */
function packageVersion() {
    const props = readFileSync(join(root, 'Directory.Build.props'), 'utf8');
    const found = props.match(/<VersionPrefix[^>]*>([\d.]+)<\/VersionPrefix>/);

    if (!found) {
        throw new Error('no VersionPrefix in Directory.Build.props.');
    }

    return found[1];
}

function writeGraph(spec) {
    const { nodes, connectors, views, groups } = buildElementGraph(spec);

    const interludeNodes = nodes
        .filter((n) => n.FunctionSignature?.startsWith('Interlude.'))
        .map((n) => n.Id);

    const graph = {
        Uuid: idFor('graph:' + spec.node),
        IsCustomNode: false,
        Description: spec.description,
        Name: spec.node,
        ElementResolver: { ResolutionMap: {} },
        Inputs: [],
        Outputs: [],
        Nodes: nodes,
        Connectors: connectors,
        Dependencies: [],
        NodeLibraryDependencies: [
            {
                Name: 'Interlude',
                Version: packageVersion(),
                ReferenceType: 'Package',
                Nodes: interludeNodes,
            },
        ],
        EnableLegacyPolyCurveBehavior: null,
        Thumbnail: '',
        GraphDocumentationURL: null,
        ExtensionWorkspaceData: [
            {
                ExtensionGuid: '28992e1d-abb9-417f-8b1b-05e053bee670',
                Name: 'Properties',
                Version: '4.2',
                Data: {},
            },
        ],
        Author: '',
        Linting: {
            activeLinter: 'None',
            activeLinterId: '7b75fb44-43fd-4631-a878-29f4d5d8399a',
            warningCount: 0,
            errorCount: 0,
        },
        Bindings: [],
        View: {
            Dynamo: {
                ScaleFactor: 1,
                HasRunWithoutCrash: true,
                IsVisibleInDynamoLibrary: true,
                Version: '4.2.0.5752',
                RunType: 'Manual',
                RunPeriod: '1000',
            },
            Camera: {
                Name: '_Background Preview',
                EyeX: -17, EyeY: 24, EyeZ: 50,
                LookX: 12, LookY: -13, LookZ: -58,
                UpX: 0, UpY: 1, UpZ: 0,
            },
            ConnectorPins: [],
            NodeViews: views,
            Annotations: groups,
            X: 120,
            Y: 260,
            Zoom: 0.62,
        },
    };

    const path = join(outputFolder, spec.node + '.dyn');
    writeFileSync(path, JSON.stringify(graph, null, 2) + '\n', 'utf8');
    return path;
}

const only = process.argv.includes('--only')
    ? process.argv[process.argv.indexOf('--only') + 1]
    : null;

mkdirSync(outputFolder, { recursive: true });

const specs = JSON.parse(readFileSync(join(root, 'docs', 'nodes', 'examples.spec.json'), 'utf8'));
let written = 0;

for (const spec of specs) {
    if (only && spec.node !== only) {
        continue;
    }

    counter = 0;
    writeGraph(spec);
    written++;
}

console.log(`wrote ${written} example graph${written === 1 ? '' : 's'} to docs/nodes`);
