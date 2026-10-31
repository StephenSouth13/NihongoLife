import music21, re, sys, json
def first_tune(path):
    t=open(path,encoding='utf-8').read()
    parts=re.split(r'(?m)^(?=X:)',t)
    tune=[p for p in parts if p.startswith('X:')][0]
    tune='\n'.join(l for l in tune.splitlines() if not l.startswith(('w:','W:','%','F:')))
    tune=tune.replace('"eind"','')
    return tune
def notes(path, transpose=0, expand=True):
    s=music21.converter.parse(first_tune(path),format='abc')
    if expand:
        try: s=s.expandRepeats()
        except Exception as e: print('norep',path,e,file=sys.stderr)
    out=[]
    for n in s.flatten().notesAndRests:
        if isinstance(n,music21.chord.Chord) and not isinstance(n,music21.harmony.ChordSymbol): n=n.notes[-1]
        if isinstance(n,music21.harmony.ChordSymbol): continue
        st=float(n.getOffsetInHierarchy(s)) if False else float(n.offset)
        st=float(s.flatten().elementOffset(n)) if n.activeSite is None else float(n.getOffsetBySite(s.flatten()))
        d=float(n.quarterLength)
        if n.isRest or d==0: continue
        m=n.pitch.midi+transpose
        if n.tie is not None and n.tie.type in('stop','continue') and out and out[-1]['midi']==m and abs(out[-1]['start']+out[-1]['duration']-st)<1e-6:
            out[-1]['duration']+=d; continue
        out.append({'midi':m,'start':round(st,4),'duration':round(d,4)})
    t0=out[0]['start']; 
    for o in out: o['start']=round(o['start']-t0,4)
    return out
if __name__=='__main__':
    for f in sys.argv[1:]:
        n=notes(f); print(f,len(n),' '.join(music21.pitch.Pitch(midi=x['midi']).nameWithOctave+':'+str(x['duration']) for x in n[:24]))
