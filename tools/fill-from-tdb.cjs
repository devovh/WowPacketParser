const fs=require("fs");
const readline=require("readline");
// Uzupelnia w wygenerowanym SQL kolumny creature_template, ktorych klient Forever nie wysyla,
// wartosciami z TDB (LEFT JOIN po entry). Uruchomienie:
//   node tools/fill-from-tdb.cjs [plik.sql] [TDB.sql]
const TDB = process.argv[3] || "C:/Users/devov/Desktop/TDB_full_forever_2026_10_14/TDB_full_world_forever_2026_10_14.sql";
const SQL = process.argv[2] || "out/forever_70938.sql";
const pre="INSERT INTO `creature_template` VALUES ";

function splitTuple(tuple){
  const out=[];let cur="",q=false;
  for(let i=0;i<tuple.length;i++){
    const ch=tuple[i];
    if(ch==="\\"&&q){cur+=ch+tuple[i+1];i++;continue;}
    if(ch==="'"){q=!q;cur+=ch;continue;}
    if(!q&&ch===","){out.push(cur);cur="";continue;}
    cur+=ch;
  }
  out.push(cur);
  return out;
}

(async()=>{
  // 1. wczytaj creature_template z TDB
  const tdb=new Map();
  const rl=readline.createInterface({input:fs.createReadStream(TDB,"utf8"),crlfDelay:Infinity});
  for await(const line of rl){
    if(!line.startsWith(pre))continue;
    const body=line.slice(pre.length);
    let depth=0,q=false,start=-1;
    for(let i=0;i<body.length;i++){
      const ch=body[i];
      if(ch==="\\"&&q){i++;continue;}
      if(ch==="'"){q=!q;continue;}
      if(q)continue;
      if(ch==="("){depth++;if(depth===1)start=i;}
      else if(ch===")"){depth--;if(depth===0&&start>=0){
        const v=splitTuple(body.slice(start+1,i));
        tdb.set(v[0].trim(),v);
        start=-1;}}
    }
  }

  // 2. przejdz plik SQL i uzupelnij tylko creature_template
  const lines=fs.readFileSync(SQL,"utf8").split("\n");
  const colsOf=l=>l.match(/\(([^)]*)\)/)[1].split(",").map(x=>x.replace(/`/g,"").trim());
  const TDB_COLS=["entry","KillCredit1","KillCredit2","name","femaleName","subname","TitleAlt","IconName","RequiredExpansion","VignetteID","faction","npcflag","speed_walk","speed_run","scale","Classification","dmgschool","BaseAttackTime","RangeAttackTime","BaseVariance","RangeVariance","unit_class","unit_flags","unit_flags2","unit_flags3","family","trainer_class","type","VehicleId","AIName","MovementType","ExperienceModifier","RacialLeader","movementId","WidgetSetID","WidgetSetUnitConditionID","RegenHealth","CreatureImmunitiesId","flags_extra","ScriptName","StringId","VerifiedBuild"];
  const FILL=["AIName","ScriptName","MovementType","RacialLeader","ExperienceModifier","WidgetSetID","WidgetSetUnitConditionID","RegenHealth","CreatureImmunitiesId","BaseVariance","RangeVariance","dmgschool","family","trainer_class","type"];
  let table=null,hdr=null,cur=null,changed=0,missing=0;
  for(let i=0;i<lines.length;i++){
    const line=lines[i];
    const m=line.match(/^INSERT INTO `([^`]+)` \(([^)]*)\)/);
    if(m){table=m[1];hdr=colsOf(line);continue;}
    if(table!=="creature_template")continue;
    if(!line.startsWith("("))continue;
    const end=line.lastIndexOf("),");
    if(end<0)continue;
    const vals=splitTuple(line.slice(1,end));
    if(vals.length!==hdr.length)continue;
    const entry=vals[hdr.indexOf("entry")].trim();
    const src=tdb.get(entry);
    if(!src){missing++;continue;}
    let ch=false;
    for(const c of FILL){
      const k=hdr.indexOf(c);
      const t=TDB_COLS.indexOf(c);
      if(k<0||t<0)continue;
      const cur=vals[k].trim();
      const sv=src[t];
      if(sv===undefined)continue;
      if(cur==="NULL" && sv!=="NULL" && sv!=="''"){
        vals[k]=sv; ch=true;
      }
    }
    if(ch){lines[i]="("+vals.join(",")+"),"+line.slice(end+2);changed++;}
  }
  fs.writeFileSync(SQL,lines.join("\n"),"utf8");
  console.log("wierszy uzupelnionych z TDB:",changed,"| brak w TDB:",missing);
})();
