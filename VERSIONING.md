# Versioning and release candidates

Public versions use `3.<parser-line>.<product-patch>`.

- `3` is the product family.
- The parser line changes whenever replay decoding or mapped parser output changes.
- Each product patch advances independently while it uses the same parser line.
- A parser-line change resets the parser and every product patch to `0`.

`Directory.Build.props` is the only version manifest. The parser, mydsstats,
service, MAUI, and the combined server web/API product derive their assembly and
package versions from it. Do not add version literals to source code or project
files.

Uploads identify the decoder with `ma`, `myds`, `ser`, or `api` followed by the
three-part version. The API normalizes that value when the upload is accepted.

Release candidates use immutable component tags:

```text
parser/v3.1.0
mydsstats/v3.1.0
service/v3.1.0
maui/v3.1.0
server/v3.1.0
```

Pull requests run `eng/Validate-Versions.ps1`. An output-identical parser
refactor can retain the parser line only when golden parser tests pass and the
pull request has the controlled `parser-output-identical` label.

Component tags create checksummed artifacts and a draft GitHub release. They do
not deploy, publish the service updater release, sign packages, or submit the
MAUI package to the Microsoft Store.
