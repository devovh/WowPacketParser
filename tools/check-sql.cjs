const fs=require("fs");
const L=fs.readFileSync(process.argv[2]||"out/forever_70938.sql","utf8").split("\n");
const re=/^INSERT INTO `([^`]+)` \(([^)]*)\)/;
let table=null,hdr=0;const bad={},rows={};
for(let i=0;i<L.length;i++){
  const line=L[i];
  const m=line.match(re);
  if(m){table=m[1];hdr=m[2].split(",").length;continue;}
  if(!table)continue;
  if(/^[A-Z#\-]/.test(line)&&!line.startsWith("(")){table=null;continue;}
  if(!line.startsWith("("))continue;
  const e1=line.lastIndexOf("),"),e2=line.lastIndexOf(");");
  const end=Math.max(e1,e2);
  if(end<0)continue;
  const body=line.slice(1,end);
  let d=0,q=false,n=1;
  for(let k=0;k<body.length;k++){const ch=body[k];
    if(ch==="\\"&&q){k++;continue;}
    if(ch==="'"){q=!q;continue;}
    if(q)continue;
    if(ch==="("||ch==="[")d++;else if(ch===")"||ch==="]")d--;else if(ch===","&&d===0)n++;}
  rows[table]=(rows[table]||0)+1;
  if(n!==hdr){(bad[table]=bad[table]||[]).push((i+1)+":"+n+"/"+hdr);}
}
const out=Object.entries(bad).map(([t,v])=>t+": "+v.length+" -> "+v.slice(0,3).join(" "));
console.log(out.length?out.join("\n"):"0 ZLYCH WIERSZY");
let tot=0;for(const v of Object.values(rows))tot+=v;
console.log("sprawdzone:",tot,"wierszy w",Object.keys(rows).length,"tabelach");
