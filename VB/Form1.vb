Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Drawing
Imports System.Text
Imports System.Windows.Forms
Imports DevExpress.XtraPivotGrid
Imports System.Reflection
Imports DevExpress.XtraPivotGrid.Data
Imports DevExpress.XtraPivotGrid.ViewInfo
Imports DevExpress.Utils.Drawing

Namespace Q205054
	Partial Public Class Form1
		Inherits Form
		Public Sub New()
			InitializeComponent()
		End Sub

		Private Sub Form1_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
			' TODO: This line of code loads data into the 'nwindDataSet.ProductReports' table. You can move, or remove it, as needed.
			Me.productReportsTableAdapter.Fill(Me.nwindDataSet.ProductReports)

		End Sub

		Private Sub pivotGridControl1_MouseClick(ByVal sender As Object, ByVal e As MouseEventArgs) Handles pivotGridControl1.MouseClick
			Dim hInfo As PivotGridHitInfo = pivotGridControl1.CalcHitInfo(e.Location)
			If hInfo.HitTest = PivotGridHitTest.Value Then
				HandleValueMouseClick(hInfo.ValueInfo)
			End If
		End Sub

		Private Sub HandleValueMouseClick(ByVal e As PivotFieldValueEventArgs)
			Dim higherFields() As PivotGridField = e.GetHigherLevelFields()
			Dim higherValues(higherFields.Length - 1) As Object
			For i As Integer = 0 To higherFields.Length - 1
				higherValues(i) = e.GetHigherLevelFieldValue(higherFields(i))
			Next i

			pivotGridControl1.BeginUpdate()
			Dim otherArea As PivotArea = GetOtherArea(e.IsColumn)
			Dim otherFields As List(Of PivotGridField) = pivotGridControl1.GetFieldsByArea(otherArea)
			Dim sortOrder As Nullable(Of PivotSortOrder) = GetSummarySortOrder(e.Item)
			For i As Integer = 0 To otherFields.Count - 1
				If (Not sortOrder.HasValue) OrElse sortOrder.Value = PivotSortOrder.Descending Then
					otherFields(i).SortOrder = PivotSortOrder.Ascending
				Else
					otherFields(i).SortOrder = PivotSortOrder.Descending
				End If
				otherFields(i).SortBySummaryInfo.Field = e.DataField
				otherFields(i).SortBySummaryInfo.Conditions.Clear()
				For j As Integer = 0 To higherFields.Length - 1
					otherFields(i).SortBySummaryInfo.Conditions.Add(New PivotGridFieldSortCondition(higherFields(j), higherValues(j)))
				Next j
				If e.Field IsNot Nothing AndAlso e.Field.Area <> PivotArea.DataArea Then
					otherFields(i).SortBySummaryInfo.Conditions.Add(New PivotGridFieldSortCondition(e.Field, e.Value))
				End If
			Next i
			pivotGridControl1.EndUpdate()
		End Sub

		Private Function GetOtherArea(ByVal isColumn As Boolean) As PivotArea
			If isColumn Then
				Return PivotArea.RowArea
			Else
				Return PivotArea.ColumnArea
			End If
		End Function

		Private viewInfoPI As PropertyInfo = Nothing
		Private Function GetItem(ByVal e As PivotCustomDrawFieldValueEventArgs) As PivotFieldValueItem
			If viewInfoPI Is Nothing Then
				viewInfoPI = e.GetType().GetProperty("FieldCellViewInfo", BindingFlags.Instance Or BindingFlags.NonPublic)
			End If
			Dim viewInfo As PivotFieldsAreaCellViewInfo = CType(viewInfoPI.GetValue(e, Nothing), PivotFieldsAreaCellViewInfo)
			Return viewInfo.Item
		End Function

		Private Function GetSummarySortOrder(ByVal valueItem As PivotFieldValueItem) As Nullable(Of PivotSortOrder)
			If (Not valueItem.IsLastFieldLevel) Then
				Return Nothing
			End If
			Dim pairs As List(Of PivotGridFieldPair) = valueItem.Data.VisualItems.GetSortedBySummaryFields(valueItem.IsColumn, valueItem.Index)
			If pairs Is Nothing Then
				Return Nothing
			End If
			Dim sortOrder As Nullable(Of PivotSortOrder) = Nothing
			For Each pair As PivotGridFieldPair In pairs
				If pair.DataFieldItem IsNot valueItem.DataField Then
					Continue For
				End If
				If sortOrder.HasValue Then
					If sortOrder.Value <> pair.Field.SortOrder Then
						sortOrder = Nothing
						Exit For
					End If
				End If
				sortOrder = pair.Field.SortOrder
			Next pair
			Return sortOrder
		End Function

		Private Sub pivotGridControl1_CustomDrawFieldValue(ByVal sender As Object, ByVal e As PivotCustomDrawFieldValueEventArgs) Handles pivotGridControl1.CustomDrawFieldValue
			Dim valueItem As PivotFieldValueItem = GetItem(e)
			Dim sortOrder As Nullable(Of PivotSortOrder) = GetSummarySortOrder(valueItem)
			If Not sortOrder.HasValue Then
				Return ' proceed to standard drawing
			End If
			Dim data As PivotGridViewInfoData = CType(valueItem.Data, PivotGridViewInfoData)
			data.ActiveLookAndFeel.Painter.Header.DrawObject(e.Info)


			Dim sortInfo As New SortedShapeObjectInfoArgs()
			sortInfo.Ascending = sortOrder.GetValueOrDefault() = PivotSortOrder.Ascending
			sortInfo.Graphics = e.Graphics
			Dim sortBounds As Rectangle = data.ActiveLookAndFeel.Painter.SortedShape.CalcObjectMinBounds(sortInfo)
			sortBounds.X = e.Info.CaptionRect.Right + 2
			sortBounds.Y = e.Info.CaptionRect.Y + CInt(Fix(Math.Round(CDbl(e.Info.CaptionRect.Height - sortBounds.Height) / 2)))
			sortInfo.Bounds = sortBounds
			data.ActiveLookAndFeel.Painter.SortedShape.DrawObject(sortInfo)
			e.Handled = True
		End Sub
	End Class
End Namespace