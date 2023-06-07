"use strict";

export var GestioneAnagrListeObj = {
  objRef: null,
  gridListe: null,
  anagrListe: null,
  numeroRecords: 0,

  init: (_objRef, _anagrListe) => {
    console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneAnagrListeObj - init");

    GestioneAnagrListeObj.objRef = _objRef;
    GestioneAnagrListeObj.anagrListe = _anagrListe;

    if (GestioneAnagrListeObj.gridListe) {
      GestioneAnagrListeObj.gridListe.dispose();
    }
    $("#myGridListe").dxDataGrid({
      dataSource: _anagrListe,
      height: "100%",
      width: "100%",
//      keyExpr: ["idListino", "idLista"],
      keyExpr: "idPrimaryKey",
      editing: {
        mode: 'batch',
        allowAdding: true,
        allowUpdating: true,
        selectTextOnEditStart: true,
        startEditAction: 'click',
      },
      columns: [
        {
          caption: "Id. Lista", dataField: "idLista", alignment: "center",
          allowEditing: false,
          width: 50,
        },
        {
          caption: "Abilitata", dataField: "abilitata", alignment: "center",
          width: 60,
          dataType: "boolean",
        },
        {
          caption: "Visibile", dataField: "visibile", alignment: "center",
          width: 60,
          dataType: "boolean",
        },
        {
          caption: "Lista Distribuzione", dataField: "lista", alignment: "left",
        },
        {
          caption: "Id. Lista Padre", dataField: "idListaPadre", alignment: "center",
          width: 70,
        },
        {
          dataType: "number",
          editorOptions: {
            max: 2,
            min: 0,
            showSpinButtons: true,
          },
          caption: "Priorità", dataField: "priorità", alignment: "center",
          width: 70,
        },
        {
          caption: "E' lista padre", dataField: "ioSonoListaPadre", alignment: "center",
          width: 60,
          dataType: "boolean",
        },
        {
          caption: "Colore",
          columns: [
            {
              caption: "Sfondo", dataField: "backColor", alignment: "center",
              width: 120,
            },
            {
              caption: "Testo", dataField: "foreColor", alignment: "center",
              width: 120,
            },
          ]
        },
        {
          caption: "Stampa Scontrino",
          columns: [
            {
              dataType: "boolean",
              caption: "Tavolo", dataField: "tavolo_StampaScontrino", alignment: "center",
              width: 60,
            },
            {
              dataType: "boolean",
              caption: "Banco", dataField: "banco_StampaScontrino", alignment: "center",
              width: 60,
            },
            {
              dataType: "boolean",
              caption: "Cucina", dataField: "cucina_StampaScontrino", alignment: "center",
              width: 60,
            },
            {
              dataType: "number",
              editorOptions: {
                max: 5,
                min: 0,
                showSpinButtons: true,
              },
              caption: "Scontrini", dataField: "cucina_NumeroScontrini", alignment: "center",
              width: 70,
            },
            {
              dataType: "number",
              editorOptions: {
                max: 4,
                min: 0,
                showSpinButtons: true,
              },
              caption: "Stampante", dataField: "idStampante", alignment: "center",
              width: 70,
            },
            {
              dataType: "boolean",
              caption: "Note", dataField: "stampaNoteOrdine", alignment: "center",
              width: 60,
            },
          ]
        },
      ],
      noDataText: "Nessun Dato Disponibile",
      focusedRowEnabled: false,
      showRowLines: true,
      rowAlternationEnabled: true,
      columnAutoWidth: true,
      wordWrapEnabled: true,
      showBorders: true,
      hoverStateEnabled: true,
      onInitialized(e) {
        GestioneAnagrListeObj.gridListe = e.component;
      },
      onCellPrepared: function (e) {
        if (e.rowType === "data" && (["backColor", "foreColor"].indexOf(e.column.dataField) > -1) ) {
          e.cellElement.css("background-color", e.data.backColor);
          e.cellElement.css("color", e.data.foreColor);
        }
      },
      onRowPrepared: function (e) {
        switch (e.rowType) {
          case "header":
            break;

          case "data":
            e.rowElement.css({ height: "42px" });
            e.rowElement.find("td").css({ "vertical-align": "middle" });
            break;
        }
      },
      onInitNewRow: function (e) {
        e.data.idLista = 90 + GestioneAnagrListeObj.numeroRecords;
        GestioneAnagrListeObj.numeroRecords++;

        e.data.abilitata = false;
        e.data.visibile = false;
        e.data.lista = "Descrizione Lista - ???";
        e.data.idListaPadre = 0;
        e.data.priorità = 1;
        e.data.ioSonoListaPadre = false;
        e.data.backColor = "Lavender";
        e.data.foreColor = "Gray";
        e.data.tavolo_StampaScontrino = false;
        e.data.banco_StampaScontrino = false;
        e.data.cucina_StampaScontrino = false;
        e.data.idStampante = 0;
        e.data.stampaNoteOrdine = false;
      },
      onSaving(e) {
        if (e.changes.length) {
          GestioneAnagrListeObj.objRef.invokeMethodAsync('BatchUpdateRequest', e.changes)
            .then((_newAnagrListe) => {
              GestioneAnagrListeObj.anagrListe = _newAnagrListe;
              GestioneAnagrListeObj.gridListe.option("dataSource", _newAnagrListe);

              e.component.refresh(true).done(() => {
                e.component.cancelEditData();
                GestioneAnagrListeObj.numeroRecords = 0;
              });
            }
          );
        }
      },
    });
  },

  dispose: () => {
    try {
      GestioneAnagrListeObj.gridListe.dispose();
   
      console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneAnagrListeObj - dispose");
    } catch (error) {
      console.error(moment().format("HH:mm:ss.SSS") + " - " + " GestioneAnagrListeObj - dispose - Error: " + error);
    }
  }
};

