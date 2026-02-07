export { default as Runtime } from "../WebSharper.Core.JavaScript/Runtime.js"
import "./css/all.css"
import fsharp from "highlight.js/lib/languages/fsharp"
import core from "highlight.js/lib/core"
import diff from "highlight.js/lib/languages/diff"
import xml from "highlight.js/lib/languages/xml"
import "highlight.js/styles/atom-one-light.css"
import { CreateFuncWithArgs } from "../WebSharper.Core.JavaScript/Runtime.js"
export function Main(){
  void 0;
  return{ReplaceInDom(x){
    Run();
    x.parentNode.removeChild(x);
  }};
}
function Run(){
  let b=fsharp;
  core.registerLanguage("fsharp", b);
  let b_1=diff;
  core.registerLanguage("diff", b_1);
  let b_2=xml;
  core.registerLanguage("xml", b_2);
  globalThis.document.querySelectorAll("code[class^=language-]").forEach(CreateFuncWithArgs((_1) => core.highlightElement(_1[0])), void 0);
}
class Object { }
