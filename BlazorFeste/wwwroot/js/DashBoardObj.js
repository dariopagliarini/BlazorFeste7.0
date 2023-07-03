"use strict";

export var DashBoardObj = {
  objRef: null,
  gridListe: null,
  gridOrdini: null,
  chartCasse: null,
  anagrCasse: null,
  anagrProdotti: null,

  init: (_objRef, _anagrCasse, _anagrProdotti) => {
    console.log(moment().format("HH:mm:ss.SSS") + " - " + " DashBoardObj - init");

    DashBoardObj.objRef = _objRef;
    DashBoardObj.anagrCasse = _anagrCasse;
    DashBoardObj.anagrProdotti = _anagrProdotti;

    DashBoardObj.anagrProdotti.forEach(prodotto => {
      prodotto.nomeProdotto = prodotto.nomeProdotto.replaceAll("<CR>", " ");
    });

    if (DashBoardObj.gridListe) {
      DashBoardObj.gridListe.dispose();
    }
    $("#myGridListe").dxDataGrid({
      dataSource: [],
      height: "100%",
      width: "100%",
      keyExpr: "idLista",
      columns: [
        {
          caption: "Lista Distribuzione", dataField: "lista", alignment: "left",
          cellTemplate: function (element, info) {
            element.append("<div style='font-size:12px; font-weight:bold'>" + info.text + "</div>");
          }
        },
        {
          caption: "In Coda", dataField: "ordiniInCoda", alignment: "center", width: 80,
          cellTemplate: function (element, info) {
            element.append("<div style='font-size:20px; font-weight:bold; background-color:#dc3545; color:#000000; border-style: solid; border-width:2px;border-color: #FFFFFF'>" + (info.value > 0 ? info.text : "-") + "</div>");
          }
        },
        {
          caption: "In Corso", dataField: "ordiniInCorso", alignment: "center", width: 80,
          cellTemplate: function (element, info) {
            element.append("<div style='font-size:20px; font-weight:bold; background-color:#ffc107; color:#000000; border-style: solid; border-width:2px;border-color: #FFFFFF'>" + (info.value > 0 ? info.text : "-") + "</div>");
          }
        },
        {
          caption: "Evasi", dataField: "ordiniEvasi", alignment: "center", width: 80,
          cellTemplate: function (element, info) {
            element.append("<div style='font-size:20px; font-weight:bold; background-color:#FFFFFF; color:#000000; border-style: solid; border-width:2px;border-color: #008000'>" + (info.value > 0 ? info.text : "-") + "</div>");
          }
        }
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
        DashBoardObj.gridListe = e.component;
      },
      onRowClick: function (e) {
        if (e.rowType === "data") {
          DashBoardObj.objRef.invokeMethodAsync('OpenListaDetail', e.data);
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
    });

    if (DashBoardObj.gridOrdini) {
      DashBoardObj.gridOrdini.dispose();
    }
    $("#myGridOrdini").dxDataGrid({
      dataSource: [],
      height: "100%",
      width: "100%",
      keyExpr: "idProdotto",
      columns: [
        {
          caption: "#", dataField: "idProdotto", alignment: "center", width: 30,
        },
        {
          caption: "Prodotto", dataField: "nomeProdotto", alignment: "left", name: "Prodotto",
        },
        {
          caption: "Qtà", dataField: "quantità", width: 50,
        },
        {
          caption: "Importo", dataField: "importo", format: { type: "currency", currency: "EUR", precision: 2 }, width: 80,
        },
      ],
      summary: {
        totalItems: [
          { column: 'quantità', summaryType: 'sum', valueFormat: { type: "fixedPoint", precision: 0 }, displayFormat: "{0}" },
          { column: 'importo', summaryType: 'sum', valueFormat: { type: "currency", currency: "EUR", precision: 2 }, displayFormat: "{0}" },
        ]
      },
      noDataText: "Nessun Dato Disponibile",
      focusedRowEnabled: false,
      showRowLines: true,
      rowAlternationEnabled: true,
      columnAutoWidth: true,
      wordWrapEnabled: false,
      showBorders: true,
      hoverStateEnabled: true,
      paging: { enabled: false },
      scrolling: {
        mode: "standard" // or "virtual" | "infinite"
      },
      onInitialized(e) {
        DashBoardObj.gridOrdini = e.component;
      },
      onRowPrepared: function (e) {
        switch (e.rowType) {
          case "header":
            break;

          case "data":
            //e.rowElement.css({ height: "42px" });
            e.rowElement.find("td").css({ "vertical-align": "middle" });
            break;
        }
      },
    });

    if (DashBoardObj.chartCasse) {
      DashBoardObj.chartCasse.destroy();
    }
    DashBoardObj.chartCasse = new Chart(document.getElementById("myChartCasse"), {
      type: 'pie',
      data: {
        datasets: [{
          label: "Casse (€)",
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        title: {
          display: false,
        },
        plugins: {
          legend: {
            display: true,
            position: 'right',
            labels: {
              font: {
                size: 20
              },
            },
          },
          datalabels: {
            display: true,
            formatter: (value) => {
              return new Intl.NumberFormat('it-IT', { style: 'currency', currency: 'EUR' }).format(value);
            },
            color: "black",
            font: {
              size: 10,
            }
          },
          tooltip: {
            callbacks: {
              label: function (context) {
                let label = context.label || '';

                if (label) {
                  label += ': ';
                }
                if (context.parsed !== null) {
                  label += new Intl.NumberFormat('it-IT', { style: 'currency', currency: 'EUR' }).format(context.parsed);
                }
                return label;
              }
            }
          },
        },
      },
    });
  },

  dispose: () => {
    try {
      DashBoardObj.gridListe.dispose();
      DashBoardObj.gridOrdini.dispose();    

      const chart = Chart.getChart("myChartCasse");
      chart.destroy();

      console.log(moment().format("HH:mm:ss.SSS") + " - " + " DashBoardObj - dispose");
    } catch (error) {
      console.error(moment().format("HH:mm:ss.SSS") + " - " + " DashBoardObj - dispose - Error: " + error);
    }
  },

  update: (_dati) => {
    try {
      var dati = JSON.parse(_dati);

      if ("qryStatoCasse" in dati) {
        var _qryStatoCasse = dati["qryStatoCasse"];
        if (_qryStatoCasse.length > 0) {
          const chart = Chart.getChart("myChartCasse");

          if (chart) {
            chart.data.labels = [];
            chart.data.datasets[0].data = [];
            chart.data.datasets[0].backgroundColor = [];

            _qryStatoCasse.forEach(cassa => {
              let _cassa = DashBoardObj.anagrCasse.find(({ idCassa }) => idCassa === cassa.idCassa);

              if (_cassa) {
                chart.data.labels.push(_cassa.cassa);
                chart.data.datasets[0].backgroundColor.push(_cassa.backColor);
              }
              chart.data.datasets[0].data.push(cassa.importo);
            });
            chart.update();
          }
        }
      }

      if ("qryStatoOrdini" in dati) {
        var _qryStatoOrdini = dati["qryStatoOrdini"];
        if (_qryStatoOrdini.length > 0) {
          DashBoardObj.gridOrdini.beginUpdate();

          var actColumnCount = _qryStatoOrdini[0].statoCassa.length + 4;
          if (DashBoardObj.gridOrdini.columnCount() != actColumnCount) {
            let totalItems = [
              { column: 'quantità', summaryType: 'sum', valueFormat: { type: "fixedPoint", precision: 0 }, displayFormat: "{0}" },
              { column: 'importo', summaryType: 'sum', valueFormat: { type: "currency", currency: "EUR", precision: 2 }, displayFormat: "{0}" },
            ];
            let colonne = [
              {
                caption: "#", dataField: "idProdotto", alignment: "center", width: 30,
              },
              {
                caption: "Prodotto", dataField: "nomeProdotto", alignment: "left", name: "nomeProdotto",
              },
              {
                caption: "Qtà", dataField: "quantità", width: 50,
              },
              {
                caption: "Importo", dataField: "importo", format: { type: "currency", currency: "EUR", precision: 2 }, width: 80,
              },
            ];

            let idx = 0;
            _qryStatoOrdini[0].statoCassa.forEach(cassa => {
              let _cassa = DashBoardObj.anagrCasse.find(({ idCassa }) => idCassa === cassa.idCassa);

              let colonneCassa = [
                { caption: "Qtà", name: "c" + idx + "_Qta", dataField: 'statoCassa[' + idx + '].quantitàProdotto', format: { type: "fixedPoint", precision: 0 }, width: 50, },
                { caption: "Importo", name: "c" + idx + "_Imp", dataField: 'statoCassa[' + idx + '].importo', format: { type: "currency", currency: "EUR", precision: 2 }, width: 80, },
              ];

              if (_cassa) {
                colonne.push({ caption: _cassa.cassa, name: "cassa" + idx, visible: true, alignment: "center", columns: colonneCassa });
              } else {
                colonne.push({ caption: "Cassa #" + idx, name: "cassa" + idx, visible: true, alignment: "center", columns: colonneCassa });
              }

              totalItems.push({ column: 'c' + idx + '_Qta', summaryType: 'sum', valueFormat: { type: "fixedPoint", precision: 0 }, displayFormat: "{0}" });
              totalItems.push({ column: 'c' + idx + '_Imp', summaryType: 'sum', valueFormat: { type: "currency", currency: "EUR", precision: 2 }, displayFormat: "{0}" });

              idx++;
            });
            DashBoardObj.gridOrdini.option("columns", colonne);
            DashBoardObj.gridOrdini.option("summary.totalItems", totalItems);
          }
/*
          var ds = DashBoardObj.gridOrdini.option("dataSource");
          if (ds.length > 0) {
            var actColumnCount = ds[0].statoCassa.length + 4;

            if (DashBoardObj.gridOrdini.columnCount() != actColumnCount) {
              let totalItems = [
                { column: 'quantità', summaryType: 'sum', valueFormat: { type: "fixedPoint", precision: 0 }, displayFormat: "{0}" },
                { column: 'importo', summaryType: 'sum', valueFormat: { type: "currency", currency: "EUR", precision: 2 }, displayFormat: "{0}" },
              ];
              let colonne = [
                {
                  caption: "#", dataField: "idProdotto", alignment: "center", width: 30,
                },
                {
                  caption: "Prodotto", dataField: "nomeProdotto", alignment: "left", name: "nomeProdotto",
                },
                {
                  caption: "Qtà", dataField: "quantità", width: 50,
                },
                {
                  caption: "Importo", dataField: "importo", format: { type: "currency", currency: "EUR", precision: 2 }, width: 80,
                },
              ];

              let idx = 0;
              ds[0].statoCassa.forEach(cassa => {
                let _cassa = DashBoardObj.anagrCasse.find(({ idCassa }) => idCassa === cassa.idCassa);

                let colonneCassa = [
                  { caption: "Qtà", name: "c" + idx + "_Qta", dataField: 'statoCassa[' + idx + '].quantitàProdotto', format: { type: "fixedPoint", precision: 0 }, width: 50, },
                  { caption: "Importo", name: "c" + idx + "_Imp", dataField: 'statoCassa[' + idx + '].importo', format: { type: "currency", currency: "EUR", precision: 2 }, width: 80, },
                ];

                if (_cassa) {
                  colonne.push({ caption: _cassa.cassa, name: "cassa" + idx, visible: true, alignment: "center", columns: colonneCassa });
                } else {
                  colonne.push({ caption: "Cassa #" + idx, name: "cassa" + idx, visible: true, alignment: "center", columns: colonneCassa });
                }

                totalItems.push({ column: 'c' + idx + '_Qta', summaryType: 'sum', valueFormat: { type: "fixedPoint", precision: 0 }, displayFormat: "{0}" });
                totalItems.push({ column: 'c' + idx + '_Imp', summaryType: 'sum', valueFormat: { type: "currency", currency: "EUR", precision: 2 }, displayFormat: "{0}" });

                idx++;
              });
              DashBoardObj.gridOrdini.option("columns", colonne);
              DashBoardObj.gridOrdini.option("summary.totalItems", totalItems);
            }
          }
*/
          DashBoardObj.gridOrdini.option("dataSource", _qryStatoOrdini);
          DashBoardObj.gridOrdini.endUpdate();
        }
      }

      if ("qryStatoListe" in dati) {
        var _qryStatoListe = dati["qryStatoListe"];
        if (_qryStatoListe.length > 0) {
          DashBoardObj.gridListe.beginUpdate();
          DashBoardObj.gridListe.option("dataSource", _qryStatoListe);
          DashBoardObj.gridListe.endUpdate();
        }
      }
    } catch (error) {
      console.error(moment().format("HH:mm:ss.SSS") + " - " + " DashBoardObj - update - Error: " + error);
    }
  },
};

