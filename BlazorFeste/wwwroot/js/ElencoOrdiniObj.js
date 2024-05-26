"use strict";

export var ElencoOrdiniObj = {
  objRef: null,
  gridOrdini: null,
  gridRighe: null,

  renderGridOrdini: function (_objRef, container, ordini) {
    console.log(moment().format("HH:mm:ss.SSS") + " - " + " ElencoOrdiniObj - init");
    ElencoOrdiniObj.objRef = _objRef;

    ordini.forEach(ordine => {
      ordine.righe.forEach(riga => {
        riga.nomeProdotto = riga.nomeProdotto.replaceAll("<CR>", " ");
      });
    });

    $("#myGridOrdini").dxDataGrid({
      dataSource: ordini,
      keyExpr: "idOrdine",
      height: "100%",
      noDataText: "...",
      focusedRowEnabled: false,
      showRowLines: true,
      rowAlternationEnabled: true,
      columnAutoWidth: true,
      wordWrapEnabled: true,
      showBorders: true,
      hoverStateEnabled: true,
      columns: [
        {
          dataField: "idOrdine",
          width: 120,
//          visible: true
        },
        {
          dataField: "dataOra",
          caption: "Scontrino",
          alignment: "center",
          width: 120,
        },
        {
          caption: "Cassa",
          dataField: "cassa",
          alignment: "center",
          width: 90,
        },
        {
          caption: "Tipo Ordine",
          dataField: "tipoOrdine",
          //alignment: "center",
          width: 120,
        },
        {
          caption: "Tavolo",
          dataField: "tavolo",
          //alignment: "center",
          //width: 120,
        },
        {
          caption: "N° Coperti",
          dataField: "numeroCoperti",
          //alignment: "center",
          //width: 120,
        },
        {
          caption: "Sig.",
          dataField: "referente",
          //alignment: "center",
          //width: 120,
        },
      ],
      selection: {
        mode: 'single',
      },
      paging: {
        //pageSize: 10,
      },
      headerFilter: { visible: true },
      filterRow: {
        visible: true,
      },
      onInitialized(e) {
        ElencoOrdiniObj.gridOrdini = e.component;
      },
      onSelectionChanged(selectedItems) {
        const data = selectedItems.selectedRowsData[0];
        if (data) {
          ElencoOrdiniObj.gridRighe.option("dataSource", data.righe);
        }
      },
      onRowPrepared: function (e) {
        switch (e.rowType) {
          case "header":
            break;

          case "data":
            e.rowElement.find("td").css({ "vertical-align": "middle" });
            break;
        }
      },

    });
  },

  renderGridRighe: function (container) {
    $("#myGridRighe").dxDataGrid({
      dataSource: [],
      keyExpr: "idRiga",
      height: "100%",
      noDataText: "Selezionare una riga d'ordine dalla tabella.",
      focusedRowEnabled: false,
      showRowLines: true,
      rowAlternationEnabled: true,
      columnAutoWidth: true,
      wordWrapEnabled: true,
      showBorders: true,
      hoverStateEnabled: true,
      columns: [
        {
          caption: "#",
          dataField: "idRiga",
          width: 30,
        },
        {
          caption: "Prodotto",
          dataField: "nomeProdotto",
          alignment: "left",
          //width: 230,
        },
        {
          caption: "Qtà",
          dataField: "quantitàProdotto",
          alignment: "right",
          width: 90,
        },
        {
          caption: "Importo",
          dataField: "importo",
          alignment: "right",
          format: { type: "currency", currency: "EUR", precision: 2 },
          width: 180,
        }
      ],
      summary: {
        totalItems: [
          { column: 'quantitàProdotto', summaryType: 'sum', valueFormat: { type: "fixedPoint", precision: 0 }, displayFormat: "{0}" },
          { column: 'importo', summaryType: 'sum', valueFormat: { type: "currency", currency: "EUR", precision: 2 }, displayFormat: "{0}" },
        ]
      },

      selection: {
        mode: 'single',
      },
      paging: {
        //pageSize: 10,
      },
      filterRow: {
        visible: false,
      },
      onInitialized(e) {
        ElencoOrdiniObj.gridRighe = e.component;
      }
    });
  },

}




